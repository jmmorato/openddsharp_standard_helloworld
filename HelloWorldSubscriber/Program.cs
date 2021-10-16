﻿using HelloWorld;
using OpenDDSharp;
using OpenDDSharp.DDS;
using OpenDDSharp.OpenDDS.DCPS;
using System;
using System.Collections.Generic;

namespace HelloWorldSubscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            Ace.Init();

            DomainParticipantFactory dpf = ParticipantService.Instance.GetDomainParticipantFactory("-DCPSConfigFile", "rtps.ini", "-DCPSDebugLevel", "10", "-ORBLogFile", "LogFile.log", "-ORBDebugLevel", "10");
            DomainParticipant participant = dpf.CreateParticipant(42);
            if (participant == null)
            {
                throw new Exception("Could not create the participant");
            }

            MessageTypeSupport support = new();
            ReturnCode result = support.RegisterType(participant, support.GetTypeName());
            if (result != ReturnCode.Ok)
            {
                throw new Exception("Could not register type: " + result.ToString());
            }

            Topic topic = participant.CreateTopic("MessageTopic", support.GetTypeName());
            if (topic == null)
            {
                throw new Exception("Could not create the message topic");
            }

            Subscriber subscriber = participant.CreateSubscriber();
            if (subscriber == null)
            {
                throw new Exception("Could not create the subscriber");
            }

            DataReader reader = subscriber.CreateDataReader(topic);
            if (reader == null)
            {
                throw new Exception("Could not create the message data reader");
            }
            MessageDataReader messageReader = new(reader);

            while (true)
            {
                StatusMask mask = messageReader.StatusChanges;
                if ((mask & StatusKind.DataAvailableStatus) != 0)
                {
                    List<Message> receivedData = new();
                    List<SampleInfo> receivedInfo = new();
                    result = messageReader.Take(receivedData, receivedInfo);

                    if (result == ReturnCode.Ok)
                    {
                        bool messageReceived = false;
                        for (int i = 0; i < receivedData.Count; i++)
                        {
                            if (receivedInfo[i].ValidData)
                            {
                                Console.WriteLine(receivedData[i].Content);
                                messageReceived = true;
                            }
                        }

                        if (messageReceived)
                            break;
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
