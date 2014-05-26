using System;
using System.Windows.Documents;

// Found here: http://shevaspace.blogspot.com/2007/11/how-to-search-text-in-wpf-flowdocument.html
// Original version written by Zhou Yong
namespace Sheva.Windows.Documents
{
    /// <summary>
    /// This class encapsulates the find and replace operations for <see cref="FlowDocument"/>.
    /// </summary>
    public sealed class FindManager
    {
        private readonly FlowDocument inputDocument;
        private TextPointer currentPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindManager"/> class given the specified <see cref="FlowDocument"/> instance.
        /// </summary>
        /// <param name="document">the input document</param>
        public FindManager(FlowDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            inputDocument = document;
            currentPosition = inputDocument.ContentStart;
        }

        /// <summary>
        /// Gets and sets the offset position for the<see cref="FindManager"/>
        /// </summary>
        public TextPointer CurrentPosition
        {
            get
            {
                return currentPosition;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (value.CompareTo(inputDocument.ContentStart) < 0){
					currentPosition = inputDocument.ContentStart;
				}
				else if(value.CompareTo(inputDocument.ContentEnd) > 0)
                {
					currentPosition = inputDocument.ContentEnd;
                }
				else{
					currentPosition = value;
				}
            }
        }

        /// <summary>
        /// Find next match of the input string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <returns>The <see cref="TextRange"/> instance representing the input string.</returns>
        /// <remarks>
        /// This method will advance the <see cref="CurrentPosition"/> to next context position.
        /// </remarks>
        public TextRange FindNext(String input)
        {
            TextRange textRange = GetTextRangeFromPosition(ref currentPosition, input);
            return textRange;
        }



        /// <summary>
        /// Find the corresponding <see cref="TextRange"/> instance 
        /// representing the input string given a specified text pointer position.
        /// </summary>
        /// <param name="position">the current text position</param>
        /// <param name="input">the text to search for</param>
        /// <returns>An <see cref="TextRange"/> instance represeneting the matching string withing the text container.</returns>
        public TextRange GetTextRangeFromPosition(ref TextPointer position, String input)
        {
            TextRange textRange = null;

            while (position != null)
            {
                if (position.CompareTo(inputDocument.ContentEnd) == 0)
                {
                    break;
                }

                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    String textRun = position.GetTextInRun(LogicalDirection.Forward);
                    Int32 indexInRun = textRun.IndexOf(input, StringComparison.CurrentCultureIgnoreCase);

                    if (indexInRun >= 0)
                    {
                        position = position.GetPositionAtOffset(indexInRun);
                        TextPointer nextPointer = position.GetPositionAtOffset(input.Length);
                        textRange = new TextRange(position, nextPointer);

                        // If a none-WholeWord match is found, directly terminate the loop.
                        position = position.GetPositionAtOffset(input.Length);
                    }
                    else
                    {
                        // If a match is not found, go over to the next context position after the "textRun".
                        position = position.GetPositionAtOffset(textRun.Length);
                    }
                }
                else
                {
                    //If the current position doesn't represent a text context position, go to the next context position.
                    // This can effectively ignore the formatting or emebed element symbols.
                    position = position.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            return textRange;
        }
    }
}
