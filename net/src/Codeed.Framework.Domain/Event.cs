﻿using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Codeed.Framework.Domain
{
    public abstract class Event : Message, INotification
    {
        public DateTime Timestamp { get; private set; }

        protected Event()
        {
            Timestamp = DateTime.Now;
        }
    }
}