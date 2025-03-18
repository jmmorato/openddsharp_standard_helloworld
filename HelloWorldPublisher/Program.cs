using System;
using OpenDDSharp;
using OpenDDSharp.DDS;
using OpenDDSharp.OpenDDS.DCPS;
using HelloWorld;

namespace HelloWorldPublisher
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

            var publisher = participant.CreatePublisher();
            if (publisher == null)
            {
                throw new Exception("Could not create the publisher");
            }

            var qos = new DataWriterQos
            {
                Reliability = { Kind = ReliabilityQosPolicyKind.ReliableReliabilityQos },
                History = { Kind = HistoryQosPolicyKind.KeepAllHistoryQos },
                Durability = { Kind = DurabilityQosPolicyKind.TransientLocalDurabilityQos },
            };
            var writer = publisher.CreateDataWriter(topic, qos);
            if (writer == null)
            {
                throw new Exception("Could not create the data writer");
            }
            MessageDataWriter messageWriter = new(writer);

            Console.WriteLine("Waiting for a subscriber...");
            PublicationMatchedStatus status = new();
            do
            {
                _ = messageWriter.GetPublicationMatchedStatus(ref status);
                System.Threading.Thread.Sleep(500);
            }
            while (status.CurrentCount < 1);

            Console.WriteLine("Subscriber found, writting data....");
            messageWriter.Write(new Message
            {
                Content = "Hello, I love you, won't you tell me your name?"
            });

            Console.WriteLine("Press a key to exit...");
            Console.Read();

            participant.DeleteContainedEntities();
            dpf.DeleteParticipant(participant);
            ParticipantService.Instance.Shutdown();

            Ace.Fini();
        }
    }
}
