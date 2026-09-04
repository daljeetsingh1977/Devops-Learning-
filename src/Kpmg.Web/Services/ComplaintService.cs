using Kpmg.Web.Data;
using Kpmg.Web.Models;
using Kpmg.Web.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kpmg.Web.Services;

public record PhotoUpload(string FileName, string ContentType, long Length, Func<Stream> OpenReadStream);

public record ComplaintSubmission
{
    public ComplaintCategory Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public string ReporterName { get; init; } = string.Empty;
    public string ReporterEmail { get; init; } = string.Empty;
    public string? ReporterPhone { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? LocationDescription { get; init; }
    public IReadOnlyList<PhotoUpload> Photos { get; init; } = Array.Empty<PhotoUpload>();
}

public record SubmissionResult(bool Succeeded, Complaint? Complaint, IReadOnlyList<string> Errors)
{
    public static SubmissionResult Failure(params string[] errors) => new(false, null, errors);

    public static SubmissionResult Success(Complaint complaint) => new(true, complaint, Array.Empty<string>());
}

public interface IComplaintService
{
    Task<SubmissionResult> SubmitAsync(ComplaintSubmission submission, CancellationToken cancellationToken = default);

    Task<Complaint?> GetByRequestNumberAsync(string requestNumber, CancellationToken cancellationToken = default);

    Task<Complaint?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Complaint>> ListAsync(ComplaintStatus? status = null, CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(int id, ComplaintStatus status, string? staffNotes, CancellationToken cancellationToken = default);
}

public class ComplaintService : IComplaintService
{
    private readonly ComplaintDbContext _db;
    private readonly ICouncilRoutingService _routing;
    private readonly IPhotoStorage _photoStorage;
    private readonly IEmailSender _emailSender;
    private readonly PhotoStorageOptions _photoOptions;
    private readonly IRequestNumberGenerator _requestNumbers;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ComplaintService> _logger;

    public ComplaintService(
        ComplaintDbContext db,
        ICouncilRoutingService routing,
        IPhotoStorage photoStorage,
        IEmailSender emailSender,
        IOptions<PhotoStorageOptions> photoOptions,
        IRequestNumberGenerator requestNumbers,
        TimeProvider timeProvider,
        ILogger<ComplaintService> logger)
    {
        _db = db;
        _routing = routing;
        _photoStorage = photoStorage;
        _emailSender = emailSender;
        _photoOptions = photoOptions.Value;
        _requestNumbers = requestNumbers;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SubmissionResult> SubmitAsync(ComplaintSubmission submission, CancellationToken cancellationToken = default)
    {
        var errors = Validate(submission);
        if (errors.Count > 0)
        {
            return new SubmissionResult(false, null, errors);
        }

        var now = _timeProvider.GetUtcNow();
        var route = _routing.ResolveCouncil(submission.Latitude, submission.Longitude);

        var complaint = new Complaint
        {
            RequestNumber = await GenerateUniqueRequestNumberAsync(now, cancellationToken),
            Category = submission.Category,
            Description = submission.Description.Trim(),
            ReporterName = submission.ReporterName.Trim(),
            ReporterEmail = submission.ReporterEmail.Trim(),
            ReporterPhone = submission.ReporterPhone?.Trim(),
            Latitude = submission.Latitude,
            Longitude = submission.Longitude,
            LocationDescription = submission.LocationDescription?.Trim(),
            CouncilName = route.CouncilName,
            CouncilEmail = route.CouncilEmail,
            Status = ComplaintStatus.Submitted,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var upload in submission.Photos)
        {
            await using var stream = upload.OpenReadStream();
            var stored = await _photoStorage.SaveAsync(stream, upload.ContentType, cancellationToken);
            complaint.Photos.Add(new ComplaintPhoto
            {
                OriginalFileName = Path.GetFileName(upload.FileName),
                StoredFileName = stored.StoredFileName,
                ContentType = stored.ContentType,
                SizeInBytes = stored.SizeInBytes
            });
        }

        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync(cancellationToken);

        // A mail transport failure must not lose the citizen's report; staff can still
        // see the request in the portal and re-send it from there.
        try
        {
            await _emailSender.SendAsync(ComplaintEmailFactory.CreateCouncilNotification(complaint), cancellationToken);
            await _emailSender.SendAsync(ComplaintEmailFactory.CreateCitizenAcknowledgement(complaint), cancellationToken);
            complaint.CouncilNotified = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email complaint {RequestNumber} to {CouncilEmail}.", complaint.RequestNumber, complaint.CouncilEmail);
        }

        return SubmissionResult.Success(complaint);
    }

    public Task<Complaint?> GetByRequestNumberAsync(string requestNumber, CancellationToken cancellationToken = default)
    {
        var normalised = (requestNumber ?? string.Empty).Trim().ToUpperInvariant();
        return _db.Complaints
            .Include(c => c.Photos)
            .FirstOrDefaultAsync(c => c.RequestNumber == normalised, cancellationToken);
    }

    public Task<Complaint?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Complaints.Include(c => c.Photos).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Complaint>> ListAsync(ComplaintStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Complaints.Include(c => c.Photos).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query.OrderByDescending(c => c.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(int id, ComplaintStatus status, string? staffNotes, CancellationToken cancellationToken = default)
    {
        var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (complaint is null)
        {
            return false;
        }

        complaint.Status = status;
        complaint.StaffNotes = staffNotes;
        complaint.UpdatedAtUtc = _timeProvider.GetUtcNow();
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private List<string> Validate(ComplaintSubmission submission)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(submission.Description))
        {
            errors.Add("Please describe the issue.");
        }

        if (string.IsNullOrWhiteSpace(submission.ReporterEmail))
        {
            errors.Add("Please provide an email address so we can send you updates.");
        }

        if (submission.Latitude is < -90 or > 90 || submission.Longitude is < -180 or > 180)
        {
            errors.Add("The location captured for this report is not valid. Please share your location and try again.");
        }

        if (submission.Photos.Count > _photoOptions.MaxPhotosPerComplaint)
        {
            errors.Add($"You can attach at most {_photoOptions.MaxPhotosPerComplaint} photographs.");
        }

        foreach (var photo in submission.Photos)
        {
            if (photo.Length <= 0)
            {
                errors.Add($"'{photo.FileName}' is empty.");
            }
            else if (photo.Length > _photoOptions.MaxFileSizeInBytes)
            {
                errors.Add($"'{photo.FileName}' is larger than {_photoOptions.MaxFileSizeInBytes / (1024 * 1024)} MB.");
            }

            if (!_photoOptions.AllowedContentTypes.Contains(photo.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"'{photo.FileName}' is not a supported image type.");
            }
        }

        return errors;
    }

    private async Task<string> GenerateUniqueRequestNumberAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = _requestNumbers.Generate(now);
            var exists = await _db.Complaints.AnyAsync(c => c.RequestNumber == candidate, cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique service request number.");
    }
}
