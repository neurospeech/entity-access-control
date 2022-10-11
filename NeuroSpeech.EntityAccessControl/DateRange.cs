using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EntityAccessControl
{
    [Keyless]
    public class JsonLongValue
    {
        public long Value { get; set; }
    }

    [Keyless]
    public class JsonStringValue
    {
        public string? Value { get; set; }
    }

    [Keyless]
    public class DateRange
    {

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

    }

    public class DateRangeEntity<T>
    {
        public DateRange Range { get; set; }

        public T Entity { get; set; }
    }

}