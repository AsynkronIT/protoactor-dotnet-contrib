﻿// -----------------------------------------------------------------------
//  <copyright file="ActorFixture.cs" company="Asynkron HB">
//      Copyright (C) 2015-2018 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------


using System.Threading.Tasks;

namespace Proto.TestFixtures
{
    public static class Receivers
    {
        public static Receive EmptyReceive = c => Task.CompletedTask;
    }
}