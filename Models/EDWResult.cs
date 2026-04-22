using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.Models
{
    public class EDWResult : ObservableObject
    {
        private float? _outputFactor;
        private float? _percentDiff;
        private string _statusText = "";

        public float? OutputFactor
        {
            get => _outputFactor;
            set => SetProperty(ref _outputFactor, value);
        }

        public float? PercentDiff
        {
            get => _percentDiff;
            set => SetProperty(ref _percentDiff, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }
    }
}