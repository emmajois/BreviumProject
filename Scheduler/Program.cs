using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Scheduler;

const string token = "token=684c5569-cd45-4941-a809-c465bc501d2b";
const string baseUrl = "https://scheduling.interviews.brevium.com/api/Scheduling";

bool moreAppointments = true;

using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

//reset the test system
await resetAPIAsync(client);

//get the schedule
List<AppointmentInfo> scheduledAppointments = await GetScheduleAsync(client);

//call the queue of new appointments coming in
//while loop until a 204 response
while (moreAppointments)
{
    AppointmentRequest? appointmentToSchedule = await getNextAppointment(client);

    if (appointmentToSchedule == null)
    {
        //we got back that there aren't anymore appointments to schedule
        moreAppointments = false;
    }
    else
    {
        //try and add that appointment
        AttemptAppointmentAdd(appointmentToSchedule);
    }
}


//API Calls
static async Task resetAPIAsync(HttpClient client)
{
    StringContent content = new ("Empty Body");
    HttpResponseMessage response = await client.PostAsync($"{baseUrl}/Start?{token}", content);
}

static async Task<List<AppointmentInfo>> GetScheduleAsync(HttpClient client)
{
    await using Stream stream = await client.GetStreamAsync($"{baseUrl}/Schedule?{token}");
    List<AppointmentInfo> appointments = await JsonSerializer.DeserializeAsync<List<AppointmentInfo>>(stream) ?? new();

    return (appointments);
}

async Task<AppointmentRequest?> getNextAppointment(HttpClient client)
{
    HttpResponseMessage response = await client.GetAsync($"{baseUrl}/AppointmentRequest?{token}");
    int responseCode = (int)response.StatusCode;
    var responseBody = await response.Content.ReadAsStringAsync();

    if (responseCode == 204)
    {
        return null;
    }

    return JsonSerializer.Deserialize<AppointmentRequest>(responseBody) ?? new();
}

//Helper Functions
void AttemptAppointmentAdd(AppointmentRequest appointmentToAdd){
    AppointmentInfoRequest? appointmentToRequest = null;

    //this should never get called if it's null but we want to verify first
    if (appointmentToAdd != null)
    {
        //check their preferred dates first
        if (appointmentToAdd.preferredDays != null)
        {
            //Check each preferred date in each appointment
            foreach(string appointmentDay in appointmentToAdd.preferredDays)
            {
                DateTime dateToCheck = DateTimeOffset.Parse(appointmentDay).UtcDateTime;

                //get all the prescheduled appointments on that day
                //datetime not recogized - error to fix later
                List<AppointmentInfo> preScheduled = scheduledAppointments.Where(x => DateTime.ParseExact(x.appointmentTime, "yyyy-MM-dd'T'HH:mm:ss.ss'Z'", CultureInfo.InvariantCulture) == dateToCheck).ToList();

                //check the preferred doctors if they have them
                if (appointmentToAdd.preferredDocs != null)
                {
                    foreach(int doctor in appointmentToAdd.preferredDocs)
                    {
                        preScheduled = preScheduled.Where(x => x.doctorId == doctor).ToList();

                        //check every hour if the doctor is available by their request
                        appointmentToRequest = checkAppointmentAvailablity(preScheduled, appointmentToAdd, dateToCheck);
                        if (appointmentToRequest != null)
                        {
                            //exit and create the appointment
                        }
                    }
                }
                else
                {
                    //they don't have a preferred doctor so we'll just continue with the previous prescheduled var
                    appointmentToRequest = checkAppointmentAvailablity(preScheduled, appointmentToAdd, dateToCheck);
                    if (appointmentToRequest != null)
                    {
                        //exit and create the appointment
                    }
                }
            }
        }
        //if all their preferred dates didn't prove to be available, find their most recent appointment and try every day after, starting at 7 days out
        //check the first available day for each of their preferred doctors
        //make sure to skip weekends if we're in november or december
    }
}

AppointmentInfoRequest? checkAppointmentAvailablity(List<AppointmentInfo> datesToCheckAgainst, AppointmentRequest appointment, DateTime optimalDate)
{
    AppointmentInfoRequest? appointmentToSchedule = null;

    //first check if they are a new patient
    if (appointment.isNew == true)
    {
        //only check 3pm (15) and 4pm (16)

        //if that works for them, then we can assume that they don't have any other appointments since they are new

        //check the day is not a weekend if it's november or december

        //create the appointment if it passes all the above checks
        appointmentToSchedule = new(); 
    }
    else
    {
        //check, starting at 8, if it can be scheduled or if it is a conflict on the desired date
        for (int i = 8; i < 17; i++)
        {
            //datetime not recogized - error to fix later
            if (datesToCheckAgainst.Where(x => Int32.Parse(DateTime.ParseExact(x.appointmentTime, "yyyy-MM-dd:H:mm", CultureInfo.InvariantCulture).ToString("HH:mm"))  == i).ToList().Count() == 0 )
            {
                //it should be available at that time
                //we now need to check if they have an appointment already scheduled within a week

                //check the day is not a weekend if it's november or december

                //create the appointment if it passes all the above checks
                appointmentToSchedule = new();
            }
        }
    }

    //will return the appointment if it could succesfully make it, else it will return the null object
    return appointmentToSchedule;
}
