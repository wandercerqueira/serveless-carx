namespace Domain.Core.Licenses
{
    using System;
    using Microsoft.Azure.Documents;

    public class PlateDataDocumentLicense : Resource
    {
        public string fileName { get; set; }

        public string plateText { get; set; }

        public DateTime timeStamp { get; set; }

        public bool plateFound { get; set; }

        public bool exported { get; set; }
    }
}
