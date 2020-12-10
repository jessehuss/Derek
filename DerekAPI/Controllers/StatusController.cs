using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace DerekAPI.Controllers
{
    [RoutePrefix("api/status")]
    public class StatusController : ApiController
    {
        readonly List<int> _sensorIDs;
        StatusController()
        {
            _sensorIDs = new List<int> { 62, 194, 192, 220 };
        }
        [HttpGet]
        public HttpResponseMessage GetStatus(CancellationToken clientDisconnectToken)
        {
            var response = Request.CreateResponse();
            response.Content = new PushStreamContent(async (stream, httpContent, transportContext) =>
            {
                using (var writer = new StreamWriter(stream))
                {
                    using (var consumer = new BlockingCollection<string>())
                    {
                        var eventGeneratorTask = EventGeneratorAsync(consumer, clientDisconnectToken);
                        foreach (var @event in consumer.GetConsumingEnumerable(clientDisconnectToken))
                        {
                            await writer.WriteLineAsync("data: " + @event);
                            await writer.WriteLineAsync();
                            await writer.FlushAsync();
                        }
                        await eventGeneratorTask;
                    }
                }
            }, "text/event-stream");
            return response;
        }

        private async Task EventGeneratorAsync(BlockingCollection<string> producer, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sensorList = new List<Sensor>();
                    foreach (var sensorID in _sensorIDs)
                        sensorList.Add(GetSensorRequest(sensorID));

                    var serializedSensorList = JsonConvert.SerializeObject(sensorList);
                    producer.Add(serializedSensorList, cancellationToken);
                    await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                producer.CompleteAdding();
            }
        }

        private Sensor GetSensorRequest(int ID)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(1000);
                    var response = client.GetAsync($"http://10.0.0.{ID}/status", cts.Token).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content;

                        string responseString = responseContent.ReadAsStringAsync().Result;
                        var sensor = JsonConvert.DeserializeObject<Sensor>(responseString);

                        return sensor;
                    }
                    else
                    {
                        return new Sensor() { NodeID = ID, NodeLocation = "UNKOWN...", NodeStatus = SensorStatus.Unreachable };
                    }
                }
            }
            catch
            {
                return new Sensor() { NodeID = ID, NodeLocation = "UNKOWN...", NodeStatus = SensorStatus.Unreachable };
                
            }
        }
    }

    public class Sensor
    {
        public int NodeID { get; set; }
        public String NodeLocation { get; set; }
        public SensorStatus NodeStatus { get; set; }
    }
    public enum SensorStatus
    {
        Unreachable,
        Secured,
        Unsecured
    }
}