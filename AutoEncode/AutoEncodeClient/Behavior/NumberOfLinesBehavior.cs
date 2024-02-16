using System;
using System.Windows;
using System.Windows.Controls;

namespace AutoEncodeClient.Behavior
{
    public static class NumberOfLinesBehavior
    {
        // MAX LINES
        public static readonly DependencyProperty MaxLinesProperty =
            DependencyProperty.RegisterAttached("MaxLines", typeof(int), typeof(NumberOfLinesBehavior), new PropertyMetadata(default(int), OnMaxLinesPropertyChangedCallback));

        public static void SetMaxLines(DependencyObject element, int value) => element.SetValue(MaxLinesProperty, value);
        public static int GetMaxLines(DependencyObject element) => (int)element.GetValue(MaxLinesProperty);
        private static void OnMaxLinesPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                textBlock.MaxHeight = GetLineHeight(textBlock) * GetMaxLines(textBlock);
            }
        }

        // MIN LINES
        public static readonly DependencyProperty MinLinesProperty =
            DependencyProperty.RegisterAttached("MinLines", typeof(int), typeof(NumberOfLinesBehavior), new PropertyMetadata(default(int), OnMinLinesPropertyChangedCallback));

        public static void SetMinLines(DependencyObject element, int value) => element.SetValue(MinLinesProperty, value);
        public static int GetMinLines(DependencyObject element) => (int)element.GetValue(MinLinesProperty);
        private static void OnMinLinesPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                textBlock.MinHeight = GetLineHeight(textBlock) * GetMinLines(textBlock);
            }
        }

        private static double GetLineHeight(TextBlock textBlock)
        {
            double lineHeight = textBlock.LineHeight;
            if (double.IsNaN(lineHeight) is true)
            {
                lineHeight = Math.Ceiling(textBlock.FontSize * textBlock.FontFamily.LineSpacing);
            }

            return lineHeight;
        }
    }
}
