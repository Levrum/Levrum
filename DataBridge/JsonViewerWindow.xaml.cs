using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Win32;

using Levrum.Utils;

using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Search;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for JsonViewerWindow.xaml
    /// </summary>
    public partial class JsonViewerWindow : Window
    {
		public FoldingManager FoldingManager { get; set; } = null;
		public BraceFoldingStrategy FoldingStrategy { get; set; } = new BraceFoldingStrategy();

		public SearchPanel SearchPanel { get; protected set; } = null;

        public JsonViewerWindow()
        {
            InitializeComponent();

            TextBox.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("Json");
            FoldingManager = FoldingManager.Install(TextBox.TextArea);
			FoldingStrategy.UpdateFoldings(FoldingManager, TextBox.Document);
			SearchPanel = SearchPanel.Install(TextBox.TextArea);
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TextBox.Clear();
			Hide();
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = "*.json";
                ofd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (ofd.ShowDialog() == true)
                {
                    string json = File.ReadAllText(ofd.FileName);
                    TextBox.Text = json;
				}
            } catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception loading JSON", true);
            }
        }

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			TextBox.Clear();
			Hide();
		}

		private void TextBox_TextChanged(object sender, EventArgs e)
		{
			FoldingStrategy.UpdateFoldings(FoldingManager, TextBox.Document);
		}

		private void FindMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SearchPanel.Open();
			SearchPanel.Reactivate();
		}

		private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
		{
			string text = TextBox.SelectedText;
			bool success = false;
			int attempts = 0;
			while (success == false)
			{
				try
				{
					attempts++;
					Clipboard.SetText(text);
					success = true;
				} catch (Exception ex)
				{
					LogHelper.LogMessage(LogLevel.Warn, "Error setting clipboard text");
					if (attempts > 10)
					{
						LogHelper.LogException(ex, "Unable to set clipboard text", true);
						break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Allows producing foldings from a document based on braces.
	/// </summary>
	public class BraceFoldingStrategy
	{
		/// <summary>
		/// Gets/Sets the opening brace. The default value is '{'.
		/// </summary>
		public char OpeningBrace { get; set; }

		/// <summary>
		/// Gets/Sets the closing brace. The default value is '}'.
		/// </summary>
		public char ClosingBrace { get; set; }

		/// <summary>
		/// Creates a new BraceFoldingStrategy.
		/// </summary>
		public BraceFoldingStrategy()
		{
			this.OpeningBrace = '{';
			this.ClosingBrace = '}';
		}

		public void UpdateFoldings(FoldingManager manager, TextDocument document)
		{
			int firstErrorOffset;
			IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document, out firstErrorOffset);
			manager.UpdateFoldings(newFoldings, firstErrorOffset);
		}

		/// <summary>
		/// Create <see cref="NewFolding"/>s for the specified document.
		/// </summary>
		public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
		{
			firstErrorOffset = -1;
			return CreateNewFoldings(document);
		}

		/// <summary>
		/// Create <see cref="NewFolding"/>s for the specified document.
		/// </summary>
		public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
		{
			List<NewFolding> newFoldings = new List<NewFolding>();

			Stack<int> startOffsets = new Stack<int>();
			int lastNewLineOffset = 0;
			char openingBrace = this.OpeningBrace;
			char closingBrace = this.ClosingBrace;
			for (int i = 0; i < document.TextLength; i++)
			{
				char c = document.GetCharAt(i);
				if (c == openingBrace)
				{
					startOffsets.Push(i);
				}
				else if (c == closingBrace && startOffsets.Count > 0)
				{
					int startOffset = startOffsets.Pop();
					// don't fold if opening and closing brace are on the same line
					if (startOffset < lastNewLineOffset)
					{
						newFoldings.Add(new NewFolding(startOffset, i + 1));
					}
				}
				else if (c == '\n' || c == '\r')
				{
					lastNewLineOffset = i + 1;
				}
			}
			newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
			return newFoldings;
		}
	}
}
