﻿using MediatR;
using System;

namespace Trsys.Web.Models.WriteModel.Commands
{
    public class ClearOrdersCommand : IRequest
    {
        public ClearOrdersCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}
