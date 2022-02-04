using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroSpeech.EntityAccessControl
{
    public class FieldError
    {
        public FieldError(string name, string reason)
        {
            this.Name = name;
            this.Reason = reason;
        }
        public string Name { get; }
        public string Reason { get; }
    }

    public class ErrorModel
    {
        public string Title { get; set; } = "Access Denied";

        public string Detail
            => string.Join(",\r\n",
                ParamErrors.Select(x => x.Name == null
                    ? x.Reason
                    : $"{x.Name}: {x.Reason}"));

        public List<FieldError> ParamErrors { get; } = new List<FieldError>();

        public override string ToString()
        {
            return $"{Title}\r\n{Detail}";
        }

        public void Add(string name, string reason)
        {
            ParamErrors.Add(new FieldError(name, reason));
        }

        public static implicit operator ErrorModel(string message) => new ErrorModel {  Title = message };
    }

    public class EntityAccessException : Exception
    {
        public readonly ErrorModel ErrorModel;

        public EntityAccessException(ErrorModel model) : base(model.ToString())
        {
            this.ErrorModel = model;
        }

    }
}
