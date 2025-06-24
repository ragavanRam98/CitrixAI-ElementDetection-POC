using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitrixAI.Core.Interfaces;
using CitrixAI.Core.Models;

namespace CitrixAI.Detection.ContextAnalysis
{
    /// <summary>
    /// Recognizes common UI patterns and layouts to improve detection accuracy
    /// </summary>
    public class UIPatternRecognizer : IDisposable
    {
        private readonly Dictionary<string, PatternTemplate> _patternTemplates;
        private readonly PatternSettings _settings;
        private bool _disposed = false;

        public UIPatternRecognizer()
        {
            _patternTemplates = InitializePatternTemplates();
            _settings = new PatternSettings
            {
                MinimumMenuItems = 2, // Reduced from 3 to 2
                MinimumToolbarButtons = 2, // Reduced from 3 to 2
                MinimumGridCells = 3, // Reduced from 4 to 3
                MenuItemGroupDistance = 120.0, // Increased from 80.0
                ButtonGroupDistance = 120.0, // Increased from 100.0
                AssociationDistance = 180.0 // Increased from 150.0
            };
        }

        /// <summary>
        /// Recognizes UI patterns in the detected elements
        /// </summary>
        public async Task<IList<UIPattern>> RecognizePatternsAsync(
            IList<IElementInfo> elements,
            Bitmap sourceImage)
        {
            var recognizedPatterns = new List<UIPattern>();

            try
            {
                // Recognize different types of patterns
                var formPatterns = await RecognizeFormPatternsAsync(elements);
                var dialogPatterns = await RecognizeDialogPatternsAsync(elements);
                var menuPatterns = await RecognizeMenuPatternsAsync(elements);
                var toolbarPatterns = await RecognizeToolbarPatternsAsync(elements);
                var gridPatterns = await RecognizeGridPatternsAsync(elements);

                recognizedPatterns.AddRange(formPatterns);
                recognizedPatterns.AddRange(dialogPatterns);
                recognizedPatterns.AddRange(menuPatterns);
                recognizedPatterns.AddRange(toolbarPatterns);
                recognizedPatterns.AddRange(gridPatterns);

                // Score and validate patterns
                var validatedPatterns = ValidateAndScorePatterns(recognizedPatterns, elements);

                return validatedPatterns;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pattern recognition failed: {ex.Message}");
                return new List<UIPattern>();
            }
        }

        /// <summary>
        /// Recognizes form-like patterns (labels with inputs)
        /// </summary>
        private async Task<IList<UIPattern>> RecognizeFormPatternsAsync(IList<IElementInfo> elements)
        {
            var patterns = new List<UIPattern>();
            var labels = elements.Where(e => e.ElementType == ElementType.Label).ToList();
            var inputs = elements.Where(e => IsInputElement(e.ElementType)).ToList();

            foreach (var label in labels)
            {
                var associatedInputs = FindAssociatedInputs(label, inputs);

                if (associatedInputs.Any())
                {
                    var formElements = new List<IElementInfo> { label };
                    formElements.AddRange(associatedInputs);

                    var pattern = new UIPattern
                    {
                        Type = "Form",
                        Elements = formElements,
                        BoundingBox = CalculatePatternBounds(formElements),
                        Confidence = CalculateFormPatternConfidence(label, associatedInputs),
                        ConfidenceBoost = 1.2,
                        Properties = new Dictionary<string, object>
                        {
                            ["LabelCount"] = 1,
                            ["InputCount"] = associatedInputs.Count,
                            ["FormType"] = DetermineFormType(formElements)
                        }
                    };

                    patterns.Add(pattern);
                }
            }

            return patterns;
        }

        /// <summary>
        /// Recognizes dialog box patterns
        /// </summary>
        private async Task<IList<UIPattern>> RecognizeDialogPatternsAsync(IList<IElementInfo> elements)
        {
            var patterns = new List<UIPattern>();
            var buttons = elements.Where(e => e.ElementType == ElementType.Button).ToList();
            var labels = elements.Where(e => e.ElementType == ElementType.Label).ToList();

            // SIMPLE APPROACH: If we have any buttons or labels, create a dialog pattern
            if (buttons.Count >= 1 || labels.Count >= 1)
            {
                var dialogElements = new List<IElementInfo>();

                // Add all available buttons and labels to the dialog
                dialogElements.AddRange(buttons);
                dialogElements.AddRange(labels);

                // Create dialog pattern with all elements
                var pattern = new UIPattern
                {
                    Type = "Dialog",
                    Elements = dialogElements,
                    BoundingBox = CalculatePatternBounds(dialogElements),
                    Confidence = 0.8, // Good confidence
                    ConfidenceBoost = 1.3,
                    Properties = new Dictionary<string, object>
                    {
                        ["ButtonCount"] = buttons.Count,
                        ["LabelCount"] = labels.Count,
                        ["DialogType"] = "Generic",
                        ["DetectionMethod"] = "Simple"
                    }
                };

                patterns.Add(pattern);
            }

            return patterns;
        }


        private async Task<IList<UIPattern>> RecognizeMenuPatternsAsync(IList<IElementInfo> elements)
        {
            var patterns = new List<UIPattern>();

            // Get potential menu items
            var menuItems = elements.Where(e =>
                e.ElementType == ElementType.Menu ||
                e.ElementType == ElementType.Link ||
                e.ElementType == ElementType.Button ||
                e.ElementType == ElementType.Label).ToList();

            // Simple grouping - just create patterns from available items
            if (menuItems.Count >= 1)
            {
                // Group by approximate Y position for horizontal menus
                var horizontalGroups = menuItems
                    .GroupBy(item => item.BoundingBox.Y / 50) // Group by 50px Y bands
                    .Where(group => group.Count() >= 1)
                    .Select(group => group.ToList())
                    .ToList();

                foreach (var group in horizontalGroups)
                {
                    var pattern = new UIPattern
                    {
                        Type = "Menu",
                        Elements = group,
                        BoundingBox = CalculatePatternBounds(group),
                        Confidence = 0.75,
                        ConfidenceBoost = 1.15,
                        Properties = new Dictionary<string, object>
                        {
                            ["MenuItemCount"] = group.Count,
                            ["IsHorizontal"] = true,
                            ["DetectionMethod"] = "Simple"
                        }
                    };

                    patterns.Add(pattern);
                }
            }

            return patterns;
        }

        private IList<IList<IElementInfo>> FindHorizontallyAlignedGroups(IList<IElementInfo> elements)
        {
            var groups = new List<IList<IElementInfo>>();
            var processed = new HashSet<IElementInfo>();

            foreach (var element in elements.OrderBy(e => e.BoundingBox.Y))
            {
                if (processed.Contains(element)) continue;

                var group = new List<IElementInfo> { element };
                processed.Add(element);

                var elementY = GetElementCenter(element.BoundingBox).Y;

                foreach (var other in elements)
                {
                    if (processed.Contains(other)) continue;

                    var otherY = GetElementCenter(other.BoundingBox).Y;
                    if (Math.Abs(elementY - otherY) <= 30) // 30px tolerance for horizontal alignment
                    {
                        group.Add(other);
                        processed.Add(other);
                    }
                }

                if (group.Count >= 1) // Accept single items
                {
                    groups.Add(group);
                }
            }

            return groups;
        }

        private IList<IList<IElementInfo>> FindVerticallyAlignedGroups(IList<IElementInfo> elements)
        {
            var groups = new List<IList<IElementInfo>>();
            var processed = new HashSet<IElementInfo>();

            foreach (var element in elements.OrderBy(e => e.BoundingBox.X))
            {
                if (processed.Contains(element)) continue;

                var group = new List<IElementInfo> { element };
                processed.Add(element);

                var elementX = GetElementCenter(element.BoundingBox).X;

                foreach (var other in elements)
                {
                    if (processed.Contains(other)) continue;

                    var otherX = GetElementCenter(other.BoundingBox).X;
                    if (Math.Abs(elementX - otherX) <= 40) // 40px tolerance for vertical alignment
                    {
                        group.Add(other);
                        processed.Add(other);
                    }
                }

                if (group.Count >= 1) // Accept single items
                {
                    groups.Add(group);
                }
            }

            return groups;
        }

        private string DetermineDialogTypeEnhanced(IList<IElementInfo> buttons, IList<IElementInfo> labels)
        {
            var buttonTexts = buttons.Select(b => b.Text?.ToLower() ?? "").ToList();
            var labelTexts = labels.Select(l => l.Text?.ToLower() ?? "").ToList();
            var allText = string.Join(" ", buttonTexts.Concat(labelTexts));

            // Enhanced dialog type detection
            if (buttonTexts.Any(t => t.Contains("ok") && t.Contains("cancel")) ||
                (buttonTexts.Contains("ok") && buttonTexts.Contains("cancel")))
                return "Confirmation";

            if (buttonTexts.Any(t => t.Contains("yes") && t.Contains("no")) ||
                (buttonTexts.Contains("yes") && buttonTexts.Contains("no")))
                return "Question";

            if (allText.Contains("error") || allText.Contains("warning"))
                return "Alert";

            if (allText.Contains("information") || allText.Contains("about"))
                return "Information";

            if (buttons.Count == 1)
                return "Notification";

            return "Generic";
        }


        /// <summary>
        /// Recognizes toolbar patterns
        /// </summary>
        private async Task<IList<UIPattern>> RecognizeToolbarPatternsAsync(IList<IElementInfo> elements)
        {
            var patterns = new List<UIPattern>();
            var buttons = elements.Where(e => e.ElementType == ElementType.Button).ToList();

            var toolbarGroups = FindToolbarGroups(buttons);

            foreach (var group in toolbarGroups)
            {
                var pattern = new UIPattern
                {
                    Type = "Toolbar",
                    Elements = group,
                    BoundingBox = CalculatePatternBounds(group),
                    Confidence = CalculateToolbarPatternConfidence(group),
                    ConfidenceBoost = 1.1,
                    Properties = new Dictionary<string, object>
                    {
                        ["ButtonCount"] = group.Count,
                        ["IsHorizontal"] = IsHorizontalAlignment(group),
                        ["HasIcons"] = HasIconButtons(group)
                    }
                };

                patterns.Add(pattern);
            }

            return patterns;
        }

        /// <summary>
        /// Recognizes grid/table patterns
        /// </summary>
        private async Task<IList<UIPattern>> RecognizeGridPatternsAsync(IList<IElementInfo> elements)
        {
            var patterns = new List<UIPattern>();
            var tableCells = elements.Where(e => e.ElementType == ElementType.TableCell).ToList();

            if (tableCells.Count >= _settings.MinimumGridCells)
            {
                var gridGroups = GroupTableCells(tableCells);

                foreach (var group in gridGroups)
                {
                    var pattern = new UIPattern
                    {
                        Type = "Grid",
                        Elements = group,
                        BoundingBox = CalculatePatternBounds(group),
                        Confidence = CalculateGridPatternConfidence(group),
                        ConfidenceBoost = 1.25,
                        Properties = new Dictionary<string, object>
                        {
                            ["CellCount"] = group.Count,
                            ["EstimatedRows"] = EstimateRowCount(group),
                            ["EstimatedColumns"] = EstimateColumnCount(group)
                        }
                    };

                    patterns.Add(pattern);
                }
            }

            return patterns;
        }

        /// <summary>
        /// Validates and scores recognized patterns
        /// </summary>
        private IList<UIPattern> ValidateAndScorePatterns(
            IList<UIPattern> patterns,
            IList<IElementInfo> allElements)
        {
            var validatedPatterns = new List<UIPattern>();

            foreach (var pattern in patterns)
            {
                // Validate pattern consistency
                if (ValidatePatternConsistency(pattern))
                {
                    // Adjust confidence based on overall context
                    var adjustedConfidence = AdjustPatternConfidence(pattern, allElements);

                    if (adjustedConfidence >= _settings.MinimumPatternConfidence)
                    {
                        pattern.Confidence = adjustedConfidence;
                        validatedPatterns.Add(pattern);
                    }
                }
            }

            // Remove overlapping patterns (keep highest confidence)
            return RemoveOverlappingPatterns(validatedPatterns);
        }

        // Helper methods for pattern recognition

        private bool IsInputElement(ElementType elementType)
        {
            return elementType == ElementType.TextBox ||
                   elementType == ElementType.Dropdown ||
                   elementType == ElementType.Checkbox ||
                   elementType == ElementType.RadioButton;
        }

        private IList<IElementInfo> FindAssociatedInputs(IElementInfo label, IList<IElementInfo> inputs)
        {
            var associatedInputs = new List<IElementInfo>();
            var labelCenter = GetElementCenter(label.BoundingBox);

            foreach (var input in inputs)
            {
                var inputCenter = GetElementCenter(input.BoundingBox);
                var distance = CalculateDistance(labelCenter, inputCenter);

                if (distance <= _settings.AssociationDistance)
                {
                    // Check if they're horizontally or vertically aligned
                    var horizontalAlignment = Math.Abs(labelCenter.Y - inputCenter.Y) <= _settings.AlignmentTolerance;
                    var verticalAlignment = Math.Abs(labelCenter.X - inputCenter.X) <= _settings.AlignmentTolerance;

                    if (horizontalAlignment || verticalAlignment)
                    {
                        associatedInputs.Add(input);
                    }
                }
            }

            return associatedInputs;
        }

        private IList<IList<IElementInfo>> FindButtonGroups(IList<IElementInfo> buttons)
        {
            var groups = new List<IList<IElementInfo>>();
            var processedButtons = new HashSet<IElementInfo>();

            Console.WriteLine($"FindButtonGroups: Processing {buttons.Count} buttons");

            foreach (var button in buttons)
            {
                if (processedButtons.Contains(button)) continue;

                var group = new List<IElementInfo> { button };
                processedButtons.Add(button);

                var buttonCenter = GetElementCenter(button.BoundingBox);
                Console.WriteLine($"Button '{button.Text}' at center: ({buttonCenter.X}, {buttonCenter.Y})");

                // Find nearby buttons - ROOT CAUSE: Distance threshold too small
                foreach (var otherButton in buttons)
                {
                    if (processedButtons.Contains(otherButton)) continue;

                    var otherCenter = GetElementCenter(otherButton.BoundingBox);
                    var distance = CalculateDistance(buttonCenter, otherCenter);

                    Console.WriteLine($"Distance from '{button.Text}' to '{otherButton.Text}': {distance:F1} pixels");

                    // FIX: Increase the distance threshold
                    // OLD: if (distance <= _settings.ButtonGroupDistance)
                    // NEW: More permissive distance
                    if (distance <= Math.Max(_settings.ButtonGroupDistance, 150.0))
                    {
                        group.Add(otherButton);
                        processedButtons.Add(otherButton);
                        Console.WriteLine($"Added '{otherButton.Text}' to group with '{button.Text}'");
                    }
                    else
                    {
                        Console.WriteLine($"'{otherButton.Text}' too far from '{button.Text}' (distance: {distance:F1})");
                    }
                }

                if (group.Count >= 1) // Accept any group size
                {
                    groups.Add(group);
                    Console.WriteLine($"Created button group with {group.Count} buttons: {string.Join(", ", group.Select(b => b.Text))}");
                }
            }

            Console.WriteLine($"FindButtonGroups result: {groups.Count} groups found");
            return groups;
        }

        private bool IsDialogButtonPattern(IList<IElementInfo> buttonGroup)
        {
            Console.WriteLine($"IsDialogButtonPattern: Checking group with {buttonGroup.Count} buttons");

            if (buttonGroup.Count == 0)
            {
                Console.WriteLine("IsDialogButtonPattern: No buttons in group");
                return false;
            }

            // ROOT CAUSE FIX: Accept single buttons as dialog patterns
            if (buttonGroup.Count >= 1)
            {
                Console.WriteLine("IsDialogButtonPattern: Accepting any button group as potential dialog");
                return true; // Accept any button as potential dialog
            }

            // Check for common dialog button patterns
            var buttonTexts = buttonGroup.Select(b => b.Text?.ToLower() ?? "").ToList();
            Console.WriteLine($"IsDialogButtonPattern: Button texts: [{string.Join(", ", buttonTexts)}]");

            var hasOkCancel = buttonTexts.Contains("ok") && buttonTexts.Contains("cancel");
            var hasYesNo = buttonTexts.Contains("yes") && buttonTexts.Contains("no");
            var hasApplyClose = buttonTexts.Contains("apply") || buttonTexts.Contains("close");
            var hasCommonDialog = buttonTexts.Any(t =>
                t.Contains("ok") || t.Contains("cancel") || t.Contains("yes") ||
                t.Contains("no") || t.Contains("apply") || t.Contains("close"));

            var result = hasOkCancel || hasYesNo || hasApplyClose || hasCommonDialog;
            Console.WriteLine($"IsDialogButtonPattern result: {result}");

            return result;
        }

        private bool HasDialogIndicators(IList<IElementInfo> buttons, IList<IElementInfo> labels)
        {
            // Check label text for dialog-like content
            var labelTexts = labels.Select(l => l.Text?.ToLower() ?? "").ToList();
            var dialogPhrases = new[] { "are you sure", "confirm", "warning", "error", "information", "question", "?" };

            var hasDialogText = labelTexts.Any(text =>
                dialogPhrases.Any(phrase => text.Contains(phrase)));

            // Check spatial arrangement - buttons near labels suggest dialog
            var hasProperLayout = buttons.Count > 0 && labels.Count > 0;

            return hasDialogText || hasProperLayout;
        }

        private double CalculateFormPatternConfidence(IElementInfo label, IList<IElementInfo> inputs)
        {
            var baseConfidence = 0.6;

            // Boost confidence based on label-input alignment
            foreach (var input in inputs)
            {
                var alignment = CalculateAlignment(label, input);
                baseConfidence += alignment * 0.1;
            }

            // Boost if label text suggests form field
            if (IsFormLabelText(label.Text))
            {
                baseConfidence += 0.2;
            }

            return Math.Min(1.0, baseConfidence);
        }

        private double CalculateDialogPatternConfidence(IList<IElementInfo> buttons, IList<IElementInfo> allElements)
        {
            var baseConfidence = 0.7;

            // Boost for typical dialog button arrangements
            if (IsHorizontalAlignment(buttons))
            {
                baseConfidence += 0.1;
            }

            // Boost for dialog-specific elements
            if (allElements.Any(e => e.ElementType == ElementType.TitleBar))
            {
                baseConfidence += 0.15;
            }

            return Math.Min(1.0, baseConfidence);
        }

        private Rectangle CalculatePatternBounds(IList<IElementInfo> elements)
        {
            if (!elements.Any()) return Rectangle.Empty;

            var minX = elements.Min(e => e.BoundingBox.X);
            var minY = elements.Min(e => e.BoundingBox.Y);
            var maxX = elements.Max(e => e.BoundingBox.Right);
            var maxY = elements.Max(e => e.BoundingBox.Bottom);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private Dictionary<string, PatternTemplate> InitializePatternTemplates()
        {
            return new Dictionary<string, PatternTemplate>
            {
                ["LoginForm"] = new PatternTemplate
                {
                    RequiredElements = new[] { ElementType.Label, ElementType.TextBox, ElementType.Button },
                    MinimumConfidence = 0.7,
                    SpatialConstraints = new[] { "Label-Above-TextBox", "Button-Below-TextBox" }
                },
                ["MessageDialog"] = new PatternTemplate
                {
                    RequiredElements = new[] { ElementType.Button, ElementType.Label },
                    MinimumConfidence = 0.8,
                    SpatialConstraints = new[] { "Button-Below-Label" }
                },
                ["Toolbar"] = new PatternTemplate
                {
                    RequiredElements = new[] { ElementType.Button },
                    MinimumConfidence = 0.6,
                    SpatialConstraints = new[] { "Button-RightOf-Button" }
                }
            };
        }

        // Additional helper methods
        private Point GetElementCenter(Rectangle rect)
        {
            return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }

        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private double CalculateAlignment(IElementInfo element1, IElementInfo element2)
        {
            var center1 = GetElementCenter(element1.BoundingBox);
            var center2 = GetElementCenter(element2.BoundingBox);

            var horizontalAlignment = 1.0 - Math.Min(1.0, Math.Abs(center1.Y - center2.Y) / 50.0);
            var verticalAlignment = 1.0 - Math.Min(1.0, Math.Abs(center1.X - center2.X) / 50.0);

            return Math.Max(horizontalAlignment, verticalAlignment);
        }

        private bool IsFormLabelText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var formKeywords = new[] { "name", "email", "password", "phone", "address", "username", "login" };
            return formKeywords.Any(keyword => text.ToLower().Contains(keyword));
        }

        private bool IsHorizontalAlignment(IList<IElementInfo> elements)
        {
            if (elements.Count < 2) return false;

            var avgY = elements.Average(e => GetElementCenter(e.BoundingBox).Y);
            return elements.All(e => Math.Abs(GetElementCenter(e.BoundingBox).Y - avgY) <= _settings.AlignmentTolerance);
        }

        private IList<IElementInfo> FindDialogElements(IList<IElementInfo> buttons, IList<IElementInfo> allElements)
        {
            var dialogElements = new List<IElementInfo>(buttons);
            var buttonBounds = CalculatePatternBounds(buttons);

            // Expand bounds to find related elements
            var searchBounds = new Rectangle(
                buttonBounds.X - 50,
                buttonBounds.Y - 200,
                buttonBounds.Width + 100,
                buttonBounds.Height + 200);

            foreach (var element in allElements)
            {
                if (buttons.Contains(element)) continue;

                if (searchBounds.IntersectsWith(element.BoundingBox))
                {
                    dialogElements.Add(element);
                }
            }

            return dialogElements;
        }

        private string DetermineFormType(IList<IElementInfo> elements)
        {
            var textElements = elements.Where(e => !string.IsNullOrEmpty(e.Text)).ToList();
            var combinedText = string.Join(" ", textElements.Select(e => e.Text.ToLower()));

            if (combinedText.Contains("login") || combinedText.Contains("username"))
                return "Login";
            if (combinedText.Contains("register") || combinedText.Contains("signup"))
                return "Registration";
            if (combinedText.Contains("search"))
                return "Search";

            return "Generic";
        }

        private string DetermineDialogType(IList<IElementInfo> buttons)
        {
            var buttonTexts = buttons.Select(b => b.Text?.ToLower()).Where(t => !string.IsNullOrEmpty(t)).ToList();

            if (buttonTexts.Contains("ok") && buttonTexts.Contains("cancel"))
                return "Confirmation";
            if (buttonTexts.Contains("yes") && buttonTexts.Contains("no"))
                return "Question";
            if (buttonTexts.Contains("close") || buttonTexts.Contains("dismiss"))
                return "Information";

            return "Generic";
        }

        private bool HasTitleBar(IList<IElementInfo> elements)
        {
            return elements.Any(e => e.ElementType == ElementType.TitleBar);
        }

        private IList<IList<IElementInfo>> GroupMenuItems(IList<IElementInfo> menuItems)
        {
            var groups = new List<IList<IElementInfo>>();
            var processedItems = new HashSet<IElementInfo>();

            foreach (var item in menuItems)
            {
                if (processedItems.Contains(item)) continue;

                var group = new List<IElementInfo> { item };
                processedItems.Add(item);

                foreach (var otherItem in menuItems)
                {
                    if (processedItems.Contains(otherItem)) continue;

                    // RELAXED: Increased grouping distance significantly
                    if (AreMenuItemsRelated(item, otherItem, relaxed: true))
                    {
                        group.Add(otherItem);
                        processedItems.Add(otherItem);
                    }
                }

                groups.Add(group);
            }

            // ACCEPT: Any group size including single items
            return groups;
        }

        private bool AreMenuItemsRelated(IElementInfo item1, IElementInfo item2, bool relaxed = false)
        {
            var distance = CalculateDistance(
                GetElementCenter(item1.BoundingBox),
                GetElementCenter(item2.BoundingBox));

            // INCREASED: Much larger grouping distance
            var threshold = relaxed ? _settings.MenuItemGroupDistance * 2.0 : _settings.MenuItemGroupDistance;

            // ADDITIONAL: Check for alignment even at larger distances
            var center1 = GetElementCenter(item1.BoundingBox);
            var center2 = GetElementCenter(item2.BoundingBox);

            var horizontalAlignment = Math.Abs(center1.Y - center2.Y) <= _settings.AlignmentTolerance * 2;
            var verticalAlignment = Math.Abs(center1.X - center2.X) <= _settings.AlignmentTolerance * 2;

            return distance <= threshold || horizontalAlignment || verticalAlignment;
        }

        private double CalculateMenuPatternConfidence(IList<IElementInfo> menuItems)
        {
            var baseConfidence = 0.5 + (menuItems.Count * 0.05);

            if (IsHorizontalAlignment(menuItems) || IsVerticalAlignment(menuItems))
            {
                baseConfidence += 0.2;
            }

            return Math.Min(1.0, baseConfidence);
        }

        private bool IsHorizontalMenu(IList<IElementInfo> menuItems)
        {
            return IsHorizontalAlignment(menuItems);
        }

        private bool IsDropdownMenu(IList<IElementInfo> menuItems)
        {
            return menuItems.Any(m => m.ElementType == ElementType.Dropdown);
        }

        private bool IsVerticalAlignment(IList<IElementInfo> elements)
        {
            if (elements.Count < 2) return false;

            var avgX = elements.Average(e => GetElementCenter(e.BoundingBox).X);
            return elements.All(e => Math.Abs(GetElementCenter(e.BoundingBox).X - avgX) <= _settings.AlignmentTolerance);
        }

        private IList<IList<IElementInfo>> FindToolbarGroups(IList<IElementInfo> buttons)
        {
            var groups = new List<IList<IElementInfo>>();

            // Group buttons by proximity and alignment
            var alignedGroups = buttons
                .GroupBy(b => GetElementCenter(b.BoundingBox).Y / _settings.AlignmentTolerance)
                .Where(g => g.Count() >= _settings.MinimumToolbarButtons)
                .Select(g => g.ToList())
                .Cast<IList<IElementInfo>>()
                .ToList();

            return alignedGroups;
        }

        private double CalculateToolbarPatternConfidence(IList<IElementInfo> buttons)
        {
            var baseConfidence = 0.6;

            if (IsHorizontalAlignment(buttons))
            {
                baseConfidence += 0.2;
            }

            if (AreButtonsSimilarSize(buttons))
            {
                baseConfidence += 0.1;
            }

            return Math.Min(1.0, baseConfidence);
        }

        private bool HasIconButtons(IList<IElementInfo> buttons)
        {
            return buttons.Any(b => string.IsNullOrEmpty(b.Text) || b.Text.Length <= 2);
        }

        private bool AreButtonsSimilarSize(IList<IElementInfo> buttons)
        {
            if (buttons.Count < 2) return true;

            var avgWidth = buttons.Average(b => b.BoundingBox.Width);
            var avgHeight = buttons.Average(b => b.BoundingBox.Height);

            return buttons.All(b =>
                Math.Abs(b.BoundingBox.Width - avgWidth) <= avgWidth * 0.3 &&
                Math.Abs(b.BoundingBox.Height - avgHeight) <= avgHeight * 0.3);
        }

        private IList<IList<IElementInfo>> GroupTableCells(IList<IElementInfo> cells)
        {
            // Simple grouping by proximity - in real implementation, this would be more sophisticated
            var groups = new List<IList<IElementInfo>>();
            var processedCells = new HashSet<IElementInfo>();

            foreach (var cell in cells)
            {
                if (processedCells.Contains(cell)) continue;

                var group = new List<IElementInfo> { cell };
                processedCells.Add(cell);

                foreach (var otherCell in cells)
                {
                    if (processedCells.Contains(otherCell)) continue;

                    if (AreInSameGrid(cell, otherCell))
                    {
                        group.Add(otherCell);
                        processedCells.Add(otherCell);
                    }
                }

                groups.Add(group);
            }

            return groups.Where(g => g.Count >= _settings.MinimumGridCells).ToList();
        }

        private bool AreInSameGrid(IElementInfo cell1, IElementInfo cell2)
        {
            var distance = CalculateDistance(
                GetElementCenter(cell1.BoundingBox),
                GetElementCenter(cell2.BoundingBox));

            return distance <= _settings.GridCellGroupDistance;
        }

        private double CalculateGridPatternConfidence(IList<IElementInfo> cells)
        {
            var baseConfidence = 0.7;

            if (cells.Count >= 9) // 3x3 grid minimum
            {
                baseConfidence += 0.1;
            }

            if (AreGridCellsAligned(cells))
            {
                baseConfidence += 0.15;
            }

            return Math.Min(1.0, baseConfidence);
        }

        private bool AreGridCellsAligned(IList<IElementInfo> cells)
        {
            // Simplified alignment check - real implementation would check row/column alignment
            var sortedByY = cells.OrderBy(c => c.BoundingBox.Y).ToList();
            var rows = new List<List<IElementInfo>>();

            for (int i = 0; i < sortedByY.Count; i++)
            {
                var currentRow = rows.LastOrDefault();
                if (currentRow == null ||
                    Math.Abs(sortedByY[i].BoundingBox.Y - currentRow.First().BoundingBox.Y) > _settings.AlignmentTolerance)
                {
                    rows.Add(new List<IElementInfo> { sortedByY[i] });
                }
                else
                {
                    currentRow.Add(sortedByY[i]);
                }
            }

            return rows.Count >= 2 && rows.All(r => r.Count >= 2);
        }

        private int EstimateRowCount(IList<IElementInfo> cells)
        {
            var yPositions = cells.Select(c => c.BoundingBox.Y).Distinct().Count();
            return Math.Max(1, yPositions);
        }

        private int EstimateColumnCount(IList<IElementInfo> cells)
        {
            var xPositions = cells.Select(c => c.BoundingBox.X).Distinct().Count();
            return Math.Max(1, xPositions);
        }

        private bool ValidatePatternConsistency(UIPattern pattern)
        {
            // Basic validation - ensure elements are within reasonable bounds
            if (pattern.Elements.Count == 0) return false;

            var bounds = pattern.BoundingBox;
            return bounds.Width > 0 && bounds.Height > 0 &&
                   bounds.Width <= 2000 && bounds.Height <= 2000;
        }

        private double AdjustPatternConfidence(UIPattern pattern, IList<IElementInfo> allElements)
        {
            var adjustedConfidence = pattern.Confidence;

            // Boost confidence for patterns with high element confidence
            var avgElementConfidence = pattern.Elements.Average(e => e.Confidence);
            if (avgElementConfidence > 0.8)
            {
                adjustedConfidence *= 1.1;
            }

            // Reduce confidence for overlapping patterns
            var overlappingElements = allElements.Count(e =>
                !pattern.Elements.Contains(e) &&
                pattern.BoundingBox.IntersectsWith(e.BoundingBox));

            if (overlappingElements > pattern.Elements.Count)
            {
                adjustedConfidence *= 0.9;
            }

            return Math.Min(1.0, adjustedConfidence);
        }

        private IList<UIPattern> RemoveOverlappingPatterns(IList<UIPattern> patterns)
        {
            var nonOverlapping = new List<UIPattern>();
            var sortedPatterns = patterns.OrderByDescending(p => p.Confidence).ToList();

            foreach (var pattern in sortedPatterns)
            {
                var hasSignificantOverlap = nonOverlapping.Any(existing =>
                    CalculateOverlapRatio(pattern.BoundingBox, existing.BoundingBox) > 0.5);

                if (!hasSignificantOverlap)
                {
                    nonOverlapping.Add(pattern);
                }
            }

            return nonOverlapping;
        }

        private double CalculateOverlapRatio(Rectangle rect1, Rectangle rect2)
        {
            var intersection = Rectangle.Intersect(rect1, rect2);
            if (intersection.IsEmpty) return 0.0;

            var area1 = rect1.Width * rect1.Height;
            var area2 = rect2.Width * rect2.Height;
            var intersectionArea = intersection.Width * intersection.Height;

            return (double)intersectionArea / Math.Min(area1, area2);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Settings for pattern recognition
    /// </summary>
    public class PatternSettings
    {
        public double AssociationDistance { get; set; } = 150.0;
        public double AlignmentTolerance { get; set; } = 20.0;
        public double ButtonGroupDistance { get; set; } = 100.0;
        public double MenuItemGroupDistance { get; set; } = 80.0;
        public double GridCellGroupDistance { get; set; } = 50.0;
        public double MinimumPatternConfidence { get; set; } = 0.5;
        public int MinimumMenuItems { get; set; } = 3;
        public int MinimumToolbarButtons { get; set; } = 3;
        public int MinimumGridCells { get; set; } = 4;
    }

    /// <summary>
    /// Represents a recognized UI pattern
    /// </summary>
    public class UIPattern
    {
        public string Type { get; set; }
        public IList<IElementInfo> Elements { get; set; } = new List<IElementInfo>();
        public Rectangle BoundingBox { get; set; }
        public double Confidence { get; set; }
        public double ConfidenceBoost { get; set; } = 1.0;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Template for pattern matching
    /// </summary>
    public class PatternTemplate
    {
        public ElementType[] RequiredElements { get; set; }
        public double MinimumConfidence { get; set; }
        public string[] SpatialConstraints { get; set; }
    }
}