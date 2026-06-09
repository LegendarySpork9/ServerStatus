// Copyright © - 05/10/2025 - Toby Hunter
namespace ServerStatusSite.Converters
{
    public static class StyleConverter
    {
        /// <summary>
        /// Returns the CSS to change the component to dark mode.
        /// </summary>
        public static string GetTopBarDarkMode(bool darkMode)
        {
            return darkMode switch
            {
                true => "background-color: #3E3E3E; border: 1px solid transparent;",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns the CSS to change the component to dark mode.
        /// </summary>
        public static string GetTopNavLinkDarkMode(bool darkMode)
        {
            return darkMode switch
            {
                true => "color: white;",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns the CSS to change the component to dark mode.
        /// </summary>
        public static string GetBodyDarkMode(bool darkMode)
        {
            return darkMode switch
            {
                true => "background-color: #313131; color: #A9A9A9;",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns the CSS to change the component to dark mode.
        /// </summary>
        public static string GetNavMenuDarkMode(bool darkMode)
        {
            return darkMode switch
            {
                true => "background-color: #4E4E4E; color: white;",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns the CSS to change the component to dark mode.
        /// </summary>
        public static string GetTableDarkMode(bool darkMode)
        {
            return darkMode switch
            {
                true => "color: #A9A9A9;",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns the CSS to change the component to dark mode.
        /// </summary>
        public static string GetFormDarkMode(bool darkMode)
        {
            return darkMode switch
            {
                true => "form-dark",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns the CSS to change the component to dark mode.
        /// </summary>
        public static string GetInputDarkMode(bool darkMode)
        {
            return darkMode switch
            {
                true => "background-color: #3E3E3E; color: #A9A9A9; border: 1px solid deepskyblue;",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns the CSS to change the component to dark mode.
        /// </summary>
        public static string GetTableRowDarkMode(bool darkMode)
        {
            return darkMode switch
            {
                true => "dark-mode",
                _ => string.Empty
            };
        }
    }
}
