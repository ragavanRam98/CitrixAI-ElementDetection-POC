using CitrixAI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CitrixAI.Demo.ViewModels
{
    /// <summary>
    /// View model for displaying detection results in the UI.
    /// </summary>
    public class DetectionResultViewModel : INotifyPropertyChanged
    {

        private string _elementType;
        private double _confidence;
        private string _boundingBox;
        private string _text;
        private bool _isEnhanced;

        /// <summary>
        /// Initializes a new instance of the DetectionResultViewModel class.
        /// </summary>
        /// <param name="elementInfo">The element information to wrap.</param>
        public DetectionResultViewModel(IElementInfo elementInfo)
        {
            ElementInfo = elementInfo ?? throw new ArgumentNullException(nameof(elementInfo));
        }

        /// <summary>
        /// Gets the underlying element information.
        /// </summary>
        public IElementInfo ElementInfo { get; }

        /// <summary>
        /// Gets the element ID.
        /// </summary>
        public Guid ElementId => ElementInfo.ElementId;

        /// <summary>
        /// Gets the element type.
        /// </summary>
        public string ElementType
        {
            get => _elementType;
            set => SetProperty(ref _elementType, value);
        }

        /// <summary>
        /// Gets the confidence score.
        /// </summary>
        public double Confidence
        {
            get => _confidence;
            set => SetProperty(ref _confidence, value);
        }

        public string BoundingBox
        {
            get => _boundingBox;
            set => SetProperty(ref _boundingBox, value);
        }

        /// <summary>
        /// Gets the text content.
        /// </summary>
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        /// <summary>
        /// Gets the location as a formatted string.
        /// </summary>
        public string LocationString => $"{ElementInfo.BoundingBox.X},{ElementInfo.BoundingBox.Y}";


        /// <summary>
        /// Indicates if this detection result was enhanced using context analysis or advanced template matching
        /// </summary>
        public bool IsEnhanced
        {
            get => _isEnhanced;
            set => SetProperty(ref _isEnhanced, value);
        }

        /// <summary>
        /// Gets a display-friendly description of enhancements applied
        /// </summary>
        public string EnhancementDescription
        {
            get
            {
                if (!IsEnhanced) return "Standard Detection";
                return "Context-Aware Enhanced";
            }
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}