﻿// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Asynkron HB">
//      Copyright (C) 2015-2018 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcCore;
using Proto.Serialization.Wire;

class Program
{
    static async Task Main(string[] args)
    {
        var system = new ActorSystem();
        var context = new RootContext(system);
        
        //Registering "knownTypes" is not required, but improves performance as those messages
        //do not need to pass any typename manifest
        

        var remoteConfig = GrpcCoreRemoteConfig.BindToLocalhost(12001);
        system.WithRemote(remoteConfig);
        var wire = new WireSerializer(new[] { typeof(Ping), typeof(Pong), typeof(StartRemote), typeof(Start) });
        system.Serialization().RegisterSerializer(wire,true);
        await system.Remote().StartAsync();

        var messageCount = 1000000;
        var wg = new AutoResetEvent(false);
        var props = Props.FromProducer(() => new LocalActor(0, messageCount, wg));

        var pid = context.Spawn(props);
        var remotePid = new PID("127.0.0.1:12000", "remote");
        context.RequestAsync<Start>(remotePid, new StartRemote { Sender = pid }).Wait();

        var start = DateTime.Now;
        Console.WriteLine("Starting to send");
        var msg = new Ping();
        for (var i = 0; i < messageCount; i++)
        {
            context.Send(remotePid, msg);
        }
        wg.WaitOne();
        var elapsed = DateTime.Now - start;
        Console.WriteLine("Elapsed {0}", elapsed);

        var t = messageCount * 2.0 / elapsed.TotalMilliseconds * 1000;
        Console.WriteLine("Throughput {0} msg / sec", t);

        Console.ReadLine();
    }

    public class LocalActor : IActor
    {
        private int _count;
        private readonly int _messageCount;
        private readonly AutoResetEvent _wg;

        public LocalActor(int count, int messageCount, AutoResetEvent wg)
        {
            _count = count;
            _messageCount = messageCount;
            _wg = wg;
        }


        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Pong _:
                    _count++;
                    if (_count % 50000 == 0)
                    {
                        Console.WriteLine(_count);
                    }
                    if (_count == _messageCount)
                    {
                        _wg.Set();
                    }
                    break;
            }
            return Task.CompletedTask;
        }
    }
}