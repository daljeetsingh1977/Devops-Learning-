using System.Text;
using Kpmg.Web.Data;
using Kpmg.Web.Models;
using Kpmg.Web.Options;
using Kpmg.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kpmg.Web.Tests;

public class ComplaintServiceTests : IDisposable
{
    private readonly ComplaintDbContext _db;
    private readonly FakeEmailSender _emailSender = new();
    private readonly InMemoryPhotoStorage _photoStorage = new();
    private readonly ComplaintService _service;

    public ComplaintServiceTests()
    {
        _db = new ComplaintDbContext(new DbContextOptionsBuilder<ComplaintDbContext>()
            .UseInMemoryDatabase($"complaints-{Guid.NewGuid()}")
            .Options);

        _service = new ComplaintService(
            _db,
            new StubRouting(),
            _photoStorage,
            _emailSender,
            Microsoft.Extensions.Options.Options.Create(new PhotoStorageOptions()),
            new RequestNumberGenerator(),
            TimeProvider.System,
            NullLogger<ComplaintService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private static ComplaintSubmission ValidSubmission(params PhotoUpload[] photos) => new()
    {
        Category = ComplaintCategory.Pothole,
        Description = "Deep pothole in the middle of the lane.",
        ReporterName = "Asha Patel",
        ReporterEmail = "asha@example.com",
        Latitude = 51.5,
        Longitude = -0.14,
        LocationDescription = "Outside 14 High Street",
        Photos = photos
    };

    private static PhotoUpload Photo(string name = "pothole.jpg", string contentType = "image/jpeg")
    {
        var bytes = Encoding.UTF8.GetBytes("fake-image-bytes");
        return new PhotoUpload(name, contentType, bytes.Length, () => new MemoryStream(bytes));
    }

    [Fact]
    public async Task SubmitAsync_StoresComplaintWithRequestNumberCouncilAndPhotos()
    {
        var result = await _service.SubmitAsync(ValidSubmission(Photo(), Photo("second.png", "image/png")));

        Assert.True(result.Succeeded);
        var complaint = Assert.IsType<Complaint>(result.Complaint);
        Assert.StartsWith("CSR-", complaint.RequestNumber);
        Assert.Equal("street-faults@westminster.example.gov", complaint.CouncilEmail);
        Assert.Equal(ComplaintStatus.Submitted, complaint.Status);
        Assert.Equal(2, complaint.Photos.Count);
        Assert.Equal(2, _photoStorage.SavedFiles.Count);

        var persisted = await _db.Complaints.Include(c => c.Photos).SingleAsync();
        Assert.Equal(complaint.RequestNumber, persisted.RequestNumber);
        Assert.Equal(2, persisted.Photos.Count);
    }

    [Fact]
    public async Task SubmitAsync_EmailsTheCouncilForTheGeoLocationAndAcknowledgesTheCitizen()
    {
        var result = await _service.SubmitAsync(ValidSubmission(Photo()));

        Assert.True(result.Succeeded);
        Assert.True(result.Complaint!.CouncilNotified);

        var councilEmail = _emailSender.Sent[0];
        Assert.Equal("street-faults@westminster.example.gov", councilEmail.To);
        Assert.Contains(result.Complaint.RequestNumber, councilEmail.Subject);
        Assert.Single(councilEmail.AttachmentFileNames);

        var citizenEmail = _emailSender.Sent[1];
        Assert.Equal("asha@example.com", citizenEmail.To);
        Assert.Contains(result.Complaint.RequestNumber, citizenEmail.Body);
    }

    [Fact]
    public async Task SubmitAsync_KeepsTheComplaintWhenEmailDeliveryFails()
    {
        _emailSender.ThrowOnSend = true;

        var result = await _service.SubmitAsync(ValidSubmission());

        Assert.True(result.Succeeded);
        Assert.False(result.Complaint!.CouncilNotified);
        Assert.Equal(1, await _db.Complaints.CountAsync());
    }

    [Fact]
    public async Task SubmitAsync_RejectsUnsupportedPhotoTypes()
    {
        var result = await _service.SubmitAsync(ValidSubmission(Photo("virus.exe", "application/octet-stream")));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not a supported image type"));
        Assert.Empty(_photoStorage.SavedFiles);
        Assert.Equal(0, await _db.Complaints.CountAsync());
    }

    [Fact]
    public async Task SubmitAsync_RejectsTooManyPhotos()
    {
        var photos = Enumerable.Range(0, 6).Select(i => Photo($"photo{i}.jpg")).ToArray();

        var result = await _service.SubmitAsync(ValidSubmission(photos));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("at most 5 photographs"));
    }

    [Fact]
    public async Task SubmitAsync_RejectsAnInvalidGeoLocation()
    {
        var submission = ValidSubmission() with { Latitude = 120 };

        var result = await _service.SubmitAsync(submission);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("location captured"));
    }

    [Fact]
    public async Task GetByRequestNumberAsync_IsCaseInsensitiveAndIgnoresWhitespace()
    {
        var submitted = await _service.SubmitAsync(ValidSubmission());

        var found = await _service.GetByRequestNumberAsync($"  {submitted.Complaint!.RequestNumber.ToLowerInvariant()} ");

        Assert.NotNull(found);
        Assert.Equal(submitted.Complaint.Id, found!.Id);
    }

    [Fact]
    public async Task UpdateStatusAsync_LetsStaffProgressTheRequest()
    {
        var submitted = await _service.SubmitAsync(ValidSubmission());

        var updated = await _service.UpdateStatusAsync(submitted.Complaint!.Id, ComplaintStatus.InProgress, "Crew booked for Friday.");

        Assert.True(updated);
        var reloaded = await _service.GetByIdAsync(submitted.Complaint.Id);
        Assert.Equal(ComplaintStatus.InProgress, reloaded!.Status);
        Assert.Equal("Crew booked for Friday.", reloaded.StaffNotes);
    }

    [Fact]
    public async Task ListAsync_FiltersByStatusForTheStaffPortal()
    {
        var first = await _service.SubmitAsync(ValidSubmission());
        await _service.SubmitAsync(ValidSubmission());
        await _service.UpdateStatusAsync(first.Complaint!.Id, ComplaintStatus.Resolved, null);

        Assert.Equal(2, (await _service.ListAsync()).Count);
        var resolved = await _service.ListAsync(ComplaintStatus.Resolved);
        Assert.Equal(first.Complaint.RequestNumber, Assert.Single(resolved).RequestNumber);
    }

    private sealed class StubRouting : ICouncilRoutingService
    {
        public CouncilRoute ResolveCouncil(double latitude, double longitude) =>
            new("Westminster City Council", "street-faults@westminster.example.gov", false);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = new();

        public bool ThrowOnSend { get; set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("smtp unavailable");
            }

            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPhotoStorage : IPhotoStorage
    {
        public Dictionary<string, byte[]> SavedFiles { get; } = new();

        public async Task<StoredPhoto> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var name = $"{Guid.NewGuid():N}.bin";
            SavedFiles[name] = buffer.ToArray();
            return new StoredPhoto(name, contentType, buffer.Length);
        }

        public void Delete(string storedFileName) => SavedFiles.Remove(storedFileName);

        public Stream? OpenRead(string storedFileName) =>
            SavedFiles.TryGetValue(storedFileName, out var bytes) ? new MemoryStream(bytes) : null;
    }
}
