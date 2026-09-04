// Captures the citizen's geo location so the complaint can be routed to the right council.
(function () {
    const statusEl = document.getElementById('geo-status');
    const latitudeEl = document.getElementById('latitude');
    const longitudeEl = document.getElementById('longitude');
    const buttonEl = document.getElementById('capture-location');

    if (!statusEl || !latitudeEl || !longitudeEl) {
        return;
    }

    function setStatus(message, cssClass) {
        statusEl.textContent = message;
        statusEl.className = cssClass;
    }

    function capture() {
        if (!navigator.geolocation) {
            setStatus('Your browser cannot share a location. Please enter the coordinates manually.', 'text-danger');
            return;
        }

        setStatus('Requesting your location from the browser…', 'text-muted');
        navigator.geolocation.getCurrentPosition(
            function (position) {
                latitudeEl.value = position.coords.latitude.toFixed(6);
                longitudeEl.value = position.coords.longitude.toFixed(6);
                setStatus('Location captured: ' + latitudeEl.value + ', ' + longitudeEl.value, 'text-success');
            },
            function (error) {
                setStatus('We could not read your location (' + error.message + '). Please enter the coordinates manually.', 'text-danger');
            },
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 });
    }

    if (buttonEl) {
        buttonEl.addEventListener('click', capture);
    }

    if (!latitudeEl.value || !longitudeEl.value) {
        capture();
    } else {
        setStatus('Location captured: ' + latitudeEl.value + ', ' + longitudeEl.value, 'text-success');
    }
})();
