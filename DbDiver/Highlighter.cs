using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace DbDiver
{
    public class Highlighter
    {
        private static readonly Style ParagraphStyle = new Style(typeof(Paragraph));

        public Dictionary<Regex, Func<String,Inline>> Styles { get; set; }

        static Highlighter()
        {
            var setters = ParagraphStyle.Setters;

            setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(5, 2, 5, 2)));
        }

        public Highlighter()
        {
            Styles = new Dictionary<Regex, Func<String, Inline>>();
        }

        public FlowDocument Highlight(string text)
        {
            FlowDocument document = new FlowDocument
                {
                    Background = new SolidColorBrush(new Color { A = 255, R = 155, G = 155, B = 155 }),
                };
            Paragraph currentParagraph = new Paragraph();
            Inline styled;
            string unstyledFragment;

            document.Resources.Add(typeof(Paragraph), ParagraphStyle);

            while (Highlight(ref text, out unstyledFragment, out styled))
            {
                if (unstyledFragment.Contains('\n'))
                {
                    string[] lines = unstyledFragment.Split('\n');

                    for (int idx = 0; idx < lines.Length - 1; idx += 1)
                    {
                        if(lines[idx] != "")
                            currentParagraph.Inlines.Add(new Run(lines[idx].Trim('\r')));
                        document.Blocks.Add(currentParagraph);
                        currentParagraph = new Paragraph();
                    }

                    if (lines[lines.Length - 1] != "")
                        currentParagraph.Inlines.Add(new Run(lines[lines.Length - 1]));

                    currentParagraph.Inlines.Add(styled);
                }
                else
                {
                    currentParagraph.Inlines.Add(new Run(unstyledFragment));
                    currentParagraph.Inlines.Add(styled);
                }
            }

            currentParagraph.Inlines.Add(new Run(text));
            document.Blocks.Add(currentParagraph);

            return document;
        }

        private bool Highlight(ref string text, out string unstyledFragment, out Inline styled)
        {
            Match foundMatch = null;
            Func<String, Inline> foundStyle = null;

            foreach (var style in Styles)
            {
                var match = style.Key.Match(text);

                if (match.Success &&
                    (foundMatch == null || match.Index < foundMatch.Index || 
                     (match.Index == foundMatch.Index && match.Length > foundMatch.Length)))
                {
                    foundMatch = match;
                    foundStyle = style.Value;
                }
            }

            if (foundMatch != null)
            {
                unstyledFragment = text.Substring(0, foundMatch.Index);
                styled = foundStyle(foundMatch.Value);
                text = text.Substring(foundMatch.Index + foundMatch.Length);
            }
            else
            {
                unstyledFragment = null;
                styled = null;
            }

            return foundMatch != null;
        }
    }

    public static class Highlight
    {
        public static readonly Highlighter Plain = new Highlighter();

        public static readonly Highlighter Sql = new Highlighter
        {
            Styles = new Dictionary<Regex, Func<String, Inline>>
            {
                { Simple(@"\b(insert|delete|select|from|where|order by|asc|desc)\b"), text => new Run(text) { Foreground = new SolidColorBrush(Colors.White) } },
                { Simple(@"\b(if|case|when|else|begin|end)\b"), text => new Run(text) { Foreground = new SolidColorBrush(Colors.White) } },

                { Simple(@"\b(create|declare|set|return|as)\b"), text => new Bold(new Run(text)) },
                { Simple(@"\b(procedure|function|table)\b"), text => new Bold(new Run(text)) },

                { Simple(@"\bis[a-z]+\b"), text => new Italic(new Run(text)) },
                
                { Simple(@"\b(numeric|int|bit|datetime|n?(var)?char)(\(\d*\))?\b"), text => new Run(text) { Foreground = new SolidColorBrush(Colors.DarkBlue), FontWeight = FontWeights.Bold } },

                { Simple(@"\[[^\]]+\]"), text => new Run(text) },
                { Simple("'([^']|'')*'"), text => new Run(text) { Foreground = new SolidColorBrush(Colors.DarkRed) } },

                { Simple("@[a-z0-9_]+"), text => new Run(text) { Foreground = new SolidColorBrush(Colors.DarkBlue) } },

                { Simple("[0-9]+"), text => new Run(text) { Foreground = new SolidColorBrush(Colors.BlanchedAlmond) } },

                { Simple("--.*$"), text => new Run(text) { Foreground = new SolidColorBrush(Colors.DarkGreen) } },
            }
        };

        public static Regex Simple(string text){
            return new Regex(text, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }
    }
}
