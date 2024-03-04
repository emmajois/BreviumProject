namespace Scheduler
{
	public class AppointmentInfo
	{
		public int doctorId { get; set; }
		public int personId { get; set; }
		public string? appointmentTime { get; set; }
		public bool isNewPatientAppointment { get; set; }
	}

	public class AppointmentInfoRequest
	{
		public int requestId { get; set; }
		public int doctorId { get; set; }
		public int personId { get; set; }
		public string? appointmentTime{ get; set; }
		public bool isNewPatientAppointment { get; set; }
	}

	public class AppointmentRequest
	{
		public int requestId { get; set; }
		public int personId { get; set; }
		public List<string>? preferredDays { get; set; }
		public List<int>? preferredDocs { get; set; }
		public bool isNew { get; set; }
	}
}