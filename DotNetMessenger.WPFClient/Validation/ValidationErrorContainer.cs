using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace DotNetMessenger.WPFClient.Validation
{
    public delegate void ErrorsHaveChangedHandler(string propertyName);

    public interface IValidationErrorContainer
    {
        bool AddError(ValidationCustomError error, bool isWarning = false);
        bool RemoveError(string propertyName, string errorId);

        int ErrorCount { get; }
        System.Collections.IEnumerable GetPropertyErrors(string propertyName);

        event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    }

    public class ValidationErrorContainer : IValidationErrorContainer
    {
        public string LastPropertyValidated;

        public virtual bool AddError(ValidationCustomError error, bool isWarning = false)
        {
            Debug.Assert(error != null);
            var propertyName = error.PropertyName;

            if (!Errors.ContainsKey(propertyName))
                Errors[propertyName] = new List<ValidationCustomError>();

            LastPropertyValidated = propertyName;
            if (!Errors[propertyName].Contains(error))
            {
                if (isWarning)
                    Errors[propertyName].Add(error);
                else
                    Errors[propertyName].Insert(0, error);

                NotifyErrorsChanged(propertyName);
                return true;
            }
            else
                return false;
        }

        public virtual bool RemoveError(string propertyName, string errorId)
        {
            var error = new ValidationCustomError(propertyName, errorId, "");
            if (!Errors.ContainsKey(propertyName) || !Errors[propertyName].Contains(error))
                return false;

            Errors[propertyName].Remove(error);
            if (Errors[propertyName].Count == 0)
            {
                Errors.Remove(propertyName);
                if (Errors.Count == 0)
                    LastPropertyValidated = null; // No errors. Good.
                else
                {
                    using (var p = Errors.GetEnumerator())
                    {
                        p.MoveNext();
                        LastPropertyValidated = p.Current.Key;
                    }
                }
            }
            NotifyErrorsChanged(propertyName);
            return true;
        }

        public int ErrorCount
        {
            get { return Errors.Count; }
        }

        public void NotifyErrorsChanged(string propertyName)
        {
            if (ErrorsChanged != null)
                ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public System.Collections.IEnumerable GetPropertyErrors(string propertyName)
        {
            if (propertyName == "")
                return null;
            if (Errors.Keys.Contains(propertyName))
                return Errors[propertyName];
            else
                return null;
        }

        public readonly Dictionary<string, List<ValidationCustomError>> Errors = new Dictionary<string, List<ValidationCustomError>>();
    }
}