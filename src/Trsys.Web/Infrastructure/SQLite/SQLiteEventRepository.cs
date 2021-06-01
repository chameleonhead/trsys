﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Trsys.Web.Infrastructure.Generic;
using Trsys.Web.Models;
using Trsys.Web.Services;

namespace Trsys.Web.Infrastructure.SQLite
{
    public class SQLiteEventRepository : IEventRepository
    {
        private readonly TrsysContextProcessor processor;

        public SQLiteEventRepository(TrsysContextProcessor processor)
        {
            this.processor = processor;
        }

        public Task<List<Event>> SearchAllAsync()
        {
            return processor.Enqueue(db => new EventRepository(db).SearchAllAsync());
        }

        public Task<List<Event>> SearchAsync(string source, int page, int perPage)
        {
            return processor.Enqueue(db => new EventRepository(db).SearchAsync(source, page, perPage));
        }

        public Task SaveAsync(Event ev)
        {
            return processor.Enqueue(db => new EventRepository(db).SaveAsync(ev));
        }
    }
}
