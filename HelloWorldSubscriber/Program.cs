using System;
using System.Collections.Generic;
using OpenDDSharp;
using OpenDDSharp.DDS;
using OpenDDSharp.OpenDDS.DCPS;
using HelloWorld;

namespace HelloWorldSubscriber
{
    internal static class Program
    {
        private static void Main()
        {
            Ace.Init();

            var dpf = ParticipantService.Instance.GetDomainParticipantFactory("-DCPSConfigFile", "rtps.ini",
                "-DCPSDebugLevel", "10", "-ORBLogFile", "LogFile.log", "-ORBDebugLevel", "10");

            var participant = dpf.CreateParticipant(42);
            if (participant == null)
            {
                throw new Exception("Could not create the participant");
            }

            MessageTypeSupport support = new();
            var result = support.RegisterType(participant, support.GetTypeName());
            if (result != ReturnCode.Ok)
            {
                throw new Exception("Could not register type: " + result);
            }

            var topic = participant.CreateTopic("MessageTopic", support.GetTypeName());
            if (topic == null)
            {
                throw new Exception("Could not create the message topic");
            }

            var subscriber = participant.CreateSubscriber();
            if (subscriber == null)
            {
                throw new Exception("Could not create the subscriber");
            }

            var qos = new DataReaderQos
            {
                Reliability = { Kind = ReliabilityQosPolicyKind.ReliableReliabilityQos },
                History = { Kind = HistoryQosPolicyKind.KeepAllHistoryQos },
                Durability = { Kind = DurabilityQosPolicyKind.TransientLocalDurabilityQos },
            };
            var reader = subscriber.CreateDataReader(topic, qos);
            if (reader == null)
            {
                throw new Exception("Could not create the message data reader");
            }
            MessageDataReader messageReader = new(reader);

            while (true)
            {
                var mask = messageReader.StatusChanges;
                if ((mask & StatusKind.DataAvailableStatus) != 0)
                {
                    var receivedData = new List<Message>();
                    var receivedInfo = new List<SampleInfo>();
                    result = messageReader.Take(receivedData, receivedInfo);

                    if (result == ReturnCode.Ok)
                    {
                        var messageReceived = false;
                        for (var i = 0; i < receivedData.Count; i++)
                        {
                            if (!receivedInfo[i].ValidData)
                            {
                                continue;
                            }

                            Console.WriteLine(receivedData[i].Content);
                            messageReceived = true;
                        }

                        if (messageReceived)
                        {
                            break;
                        }
                    }
                }

                System.Threading.Thread.Sleep(100);
            }

            Console.WriteLine("Press a key to exit...");
            Console.Read();

            participant.DeleteContainedEntities();
            dpf.DeleteParticipant(participant);
            ParticipantService.Instance.Shutdown();

            Ace.Fini();
        }
    }
}
