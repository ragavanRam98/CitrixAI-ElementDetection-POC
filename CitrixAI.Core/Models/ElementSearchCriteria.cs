using CitrixAI.Core.Interfaces;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CitrixAI.Core.Models
{
    /// <summary>
    /// Implementation of IElementSearchCriteria defining search parameters.
    /// </summary>
    public class ElementSearchCriteria : IElementSearchCriteria
    {
        private readonly List<ElementType> _elementTypes;
        private readonly Dictionary<string, object> _parameters;

        /// <summary>
        /// Initializes a new instance of the ElementSearchCriteria class.
        /// </summary>
        public ElementSearchCriteria()
        {
            _elementTypes = new List<ElementType>();
            _parameters = new Dictionary<string, object>();
            MatchThreshold = 0.8;
            CaseSensitive = false;
            UseFuzzyMatching = true;
        }

        /// <inheritdoc />
        public IReadOnlyList<ElementType> ElementTypes => _elementTypes.AsReadOnly();

        /// <inheritdoc />
        public Bitmap TemplateImage { get; set; }

        /// <inheritdoc />
        public string SearchText { get; set; }

        /// <inheritdoc />
        public double MatchThreshold { get; set; }

        /// <inheritdoc />
        public IDictionary<string, object> Parameters => new Dictionary<string, object>(_parameters);

        /// <inheritdoc />
        public bool CaseSensitive { get; set; }

        /// <inheritdoc />
        public bool UseFuzzyMatching { get; set; }

        /// <summary>
        /// Sets the element types to search for.
        /// </summary>
        /// <param name="elementTypes">The element types to search for.</param>
        public void SetElementTypes(IEnumerable<ElementType> elementTypes)
        {
            _elementTypes.Clear();
            if (elementTypes != null)
            {
                _elementTypes.AddRange(elementTypes);
            }
        }

        /// <summary>
        /// Adds element types to search for.
        /// </summary>
        /// <param name="elementTypes">The element types to add.</param>
        public void AddElementTypes(params ElementType[] elementTypes)
        {
            if (elementTypes != null)
            {
                _elementTypes.AddRange(elementTypes);
            }
        }

        /// <summary>
        /// Sets a search parameter.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        public void SetParameter(string key, object value)
        {
            _parameters[key] = value;
        }
    }
}