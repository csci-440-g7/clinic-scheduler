namespace ClinicScheduler.Shared.Services;

/// <summary>
/// Mutable demo data (per session). Seeded from <see cref="MockClinicData"/>.
/// </summary>
public class ClinicDataStore
{
    private readonly List<MockClinicData.Patient> _patients;
    private readonly List<MockClinicData.Note> _notes;
    private readonly List<MockClinicData.Appointment> _appointments;

    public ClinicDataStore()
    {
        _patients = MockClinicData.Patients.Select(p => p).ToList();
        _notes = MockClinicData.Notes.Select(n => n).ToList();
        _appointments = MockClinicData.Appointments.Select(a => a).ToList();
    }

    public IReadOnlyList<MockClinicData.Patient> Patients => _patients;

    public MockClinicData.Patient? GetPatient(string id) =>
        _patients.FirstOrDefault(p => p.Id == id);

    public void AddPatient(MockClinicData.Patient p) => _patients.Add(p);

    public void UpdatePatient(MockClinicData.Patient p)
    {
        var i = _patients.FindIndex(x => x.Id == p.Id);
        if (i >= 0)
        {
            _patients[i] = p;
        }
    }

    public IReadOnlyList<MockClinicData.Appointment> Appointments => _appointments;

    public MockClinicData.Appointment? GetAppointment(string id) =>
        _appointments.FirstOrDefault(a => a.Id == id);

    public void AddAppointment(MockClinicData.Appointment a) => _appointments.Add(a);

    public void UpdateAppointmentStatus(string id, string status)
    {
        var i = _appointments.FindIndex(a => a.Id == id);
        if (i >= 0)
            _appointments[i] = _appointments[i] with { Status = status };
    }

    public void UpdateAppointmentMeetingNotes(string id, string? meetingNotes)
    {
        var i = _appointments.FindIndex(a => a.Id == id);
        if (i < 0)
        {
            return;
        }

        _appointments[i] = _appointments[i] with { MeetingNotes = meetingNotes };
    }

    public List<MockClinicData.Note> NotesForDoctor(string doctorId) =>
        _notes.Where(n => n.DoctorId == doctorId).OrderByDescending(n => n.Date).ToList();

    public List<MockClinicData.Note> NotesForPatient(string patientId) =>
        _notes.Where(n => n.PatientId == patientId).OrderByDescending(n => n.Date).ToList();

    public void UpdateNote(string id, string text)
    {
        var i = _notes.FindIndex(n => n.Id == id);
        if (i < 0)
        {
            return;
        }

        _notes[i] = _notes[i] with { Text = text };
    }
}
