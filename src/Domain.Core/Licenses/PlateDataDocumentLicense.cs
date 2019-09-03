namespace Domain.Core.Licenses
{
    using System;
    using Microsoft.Azure.Documents;

    public class PlateDataDocumentLicense : Resource
    {
        public string FileName { get; set; }

        public string PlateText { get; set; }

        public DateTime TimeStamp { get; set; }

        public bool PlateFound { get; set; }

        public bool Exported { get; set; }
    }
}
