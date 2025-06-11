namespace CitrixAI.Core.Interfaces
{
    /// <summary>
    /// Defines the types of UI elements that can be detected.
    /// </summary>
    public enum ElementType
    {
        /// <summary>
        /// Unknown or unclassified element type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Clickable button element.
        /// </summary>
        Button = 1,

        /// <summary>
        /// Text input field or textbox.
        /// </summary>
        TextBox = 2,

        /// <summary>
        /// Static text label.
        /// </summary>
        Label = 3,

        /// <summary>
        /// Dropdown or combobox control.
        /// </summary>
        Dropdown = 4,

        /// <summary>
        /// Checkbox control.
        /// </summary>
        Checkbox = 5,

        /// <summary>
        /// Radio button control.
        /// </summary>
        RadioButton = 6,

        /// <summary>
        /// Table or grid control.
        /// </summary>
        Table = 7,

        /// <summary>
        /// Table cell within a table.
        /// </summary>
        TableCell = 8,

        /// <summary>
        /// Menu or menu item.
        /// </summary>
        Menu = 9,

        /// <summary>
        /// Tab control or tab header.
        /// </summary>
        Tab = 10,

        /// <summary>
        /// Dialog box or modal window.
        /// </summary>
        Dialog = 11,

        /// <summary>
        /// Panel or container element.
        /// </summary>
        Panel = 12,

        /// <summary>
        /// Image or icon element.
        /// </summary>
        Image = 13,

        /// <summary>
        /// Hyperlink element.
        /// </summary>
        Link = 14,

        /// <summary>
        /// Scrollbar element.
        /// </summary>
        Scrollbar = 15,

        /// <summary>
        /// Window title bar.
        /// </summary>
        TitleBar = 16,

        /// <summary>
        /// Toolbar element.
        /// </summary>
        Toolbar = 17,

        /// <summary>
        /// Status bar element.
        /// </summary>
        StatusBar = 18,

        /// <summary>
        /// Tree view control.
        /// </summary>
        TreeView = 19,

        /// <summary>
        /// List view or list control.
        /// </summary>
        ListView = 20
    }
}