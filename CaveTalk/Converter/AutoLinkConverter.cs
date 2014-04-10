﻿namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Windows.Data;
	using System.Globalization;
	using System.Windows.Controls;
	using System.Windows.Markup;
	using System.Text.RegularExpressions;

	public class AutoLinkConverter : IValueConverter {
		private const String textBlockFormat = @"<TextBlock xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">{0}</TextBlock>";

		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture) {
			var text = value as String;
			if (text == null) {
				return Binding.DoNothing;
			}

			try {
				var escapedText = text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace("{", "{}{");
				var lineBreakText = escapedText.Replace("\n", "<LineBreak />");
				var autolinkedText = Regex.Replace(lineBreakText, @"(?:http|https|ftp):\/\/[\w!?=&,.\/\+:;#~%-\{\}]+(?![\w\s!?&,.\/\+:;#~%""=-\{\}]*>)", "<Hyperlink NavigateUri=\"$&\"><Run>$&</Run><Hyperlink.ToolTip>Loading ...</Hyperlink.ToolTip></Hyperlink>", RegexOptions.Multiline);
				var xaml = String.Format(textBlockFormat, autolinkedText);
				return (TextBlock)XamlReader.Parse(xaml);
			} catch (XamlParseException) {
				return text;
			}
		}

		public object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

	}
}
