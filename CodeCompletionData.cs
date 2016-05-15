/*
 * Erstellt mit SharpDevelop.
 * Benutzer: grunwald
 * Datum: 27.08.2007
 * Zeit: 14:25
 *
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */

using System;
using System.IO;
using System.Text;
using System.Xml;

using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Dom.CSharp;
using ICSharpCode.SharpDevelop.Dom.VBNet;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using System.Text.RegularExpressions;

namespace CSharpEditor
{
	/// <summary>
	/// Represents an item in the code completion window.
	/// </summary>
	class CodeCompletionData : DefaultCompletionData, ICompletionData
	{
		IMember member;
		IClass c;
		static VBNetAmbience vbAmbience = new VBNetAmbience();
		static CSharpAmbience csharpAmbience = new CSharpAmbience();
        static readonly Regex whitespace = new Regex(@"\s+");

        public CodeCompletionData(IMember member)
			: base(member.Name, null, GetMemberImageIndex(member))
		{
			this.member = member;
		}

		public CodeCompletionData(IClass c)
			: base(c.Name, null, GetClassImageIndex(c))
		{
			this.c = c;
		}

		int overloads = 0;

		public void AddOverload()
		{
			overloads++;
		}

		static int GetMemberImageIndex(IMember member)
		{
			// Missing: different icons for private/public member
			if (member is IMethod)
				return 1;
			if (member is IProperty)
				return 2;
			if (member is IField)
				return 3;
			if (member is IEvent)
				return 6;
			return 3;
		}

        [Obsolete("Use 'ConvertDocumentation' instead.")]
        public static string GetDocumentation(string doc)
        {
            return ConvertDocumentation(doc);
        }

        /// <summary>
		/// Converts the xml documentation string into a plain text string.
		/// </summary>
		public static string ConvertDocumentation(string doc)
        {
            if (string.IsNullOrEmpty(doc))
                return string.Empty;

            System.IO.StringReader reader = new System.IO.StringReader("<docroot>" + doc + "</docroot>");
            XmlTextReader xml = new XmlTextReader(reader);
            StringBuilder ret = new StringBuilder();
            ////Regex whitespace    = new Regex(@"\s+");

            try
            {
                xml.Read();
                do
                {
                    if (xml.NodeType == XmlNodeType.Element)
                    {
                        string elname = xml.Name.ToLowerInvariant();
                        switch (elname)
                        {
                            case "filterpriority":
                                xml.Skip();
                                break;
                            case "remarks":
                                ret.Append(Environment.NewLine);
                                ret.Append("Remarks:");
                                ret.Append(Environment.NewLine);
                                break;
                            case "example":
                                ret.Append(Environment.NewLine);
                                ret.Append("Example:");
                                ret.Append(Environment.NewLine);
                                break;
                            case "exception":
                                ret.Append(Environment.NewLine);
                                ret.Append(GetCref(xml["cref"]));
                                ret.Append(": ");
                                break;
                            case "returns":
                                ret.Append(Environment.NewLine);
                                ret.Append("Returns: ");
                                break;
                            case "see":
                                ret.Append(GetCref(xml["cref"]));
                                ret.Append(xml["langword"]);
                                break;
                            case "seealso":
                                ret.Append(Environment.NewLine);
                                ret.Append("See also: ");
                                ret.Append(GetCref(xml["cref"]));
                                break;
                            case "paramref":
                                ret.Append(xml["name"]);
                                break;
                            case "param":
                                ret.Append(Environment.NewLine);
                                ret.Append(whitespace.Replace(xml["name"].Trim(), " "));
                                ret.Append(": ");
                                break;
                            case "value":
                                ret.Append(Environment.NewLine);
                                ret.Append("Value: ");
                                ret.Append(Environment.NewLine);
                                break;
                            case "br":
                            case "para":
                                ret.Append(Environment.NewLine);
                                break;
                        }
                    }
                    else if (xml.NodeType == XmlNodeType.Text)
                    {
                        ret.Append(whitespace.Replace(xml.Value, " "));
                    }
                } while (xml.Read());
            }
            catch (Exception ex)
            {
                //LoggingService.Debug("Invalid XML documentation: " + ex.Message);
                //return doc;
            }
            return ret.ToString();
        }

        static string GetCref(string cref)
        {
            if (cref == null || cref.Trim().Length == 0)
            {
                return "";
            }
            if (cref.Length < 2)
            {
                return cref;
            }
            if (cref.Substring(1, 1) == ":")
            {
                return cref.Substring(2, cref.Length - 2);
            }
            return cref;
        }

        static int GetClassImageIndex(IClass c)
		{
			switch (c.ClassType) {
				case ClassType.Enum:
					return 4;
				default:
					return 0;
			}
		}

		string description;

		// DefaultCompletionData.Description is not virtual, but we can reimplement
		// the interface to get the same effect as overriding.
		string ICompletionData.Description {
			get {
				if (description == null) {
					IEntity entity = (IEntity)member ?? c;
					description = GetText(entity);
					if (overloads > 1) {
						description += " (+" + overloads + " overloads)";
					}
					description += Environment.NewLine + XmlDocumentationToText(entity.Documentation);
				}
				return description;
			}
		}

		/// <summary>
		/// Converts a member to text.
		/// Returns the declaration of the member as C# or VB code, e.g.
		/// "public void MemberName(string parameter)"
		/// </summary>
		static string GetText(IEntity entity)
		{
			IAmbience ambience = MainForm.IsVisualBasic ? (IAmbience)vbAmbience : csharpAmbience;
			if (entity is IMethod)
				return ambience.Convert(entity as IMethod);
			if (entity is IProperty)
				return ambience.Convert(entity as IProperty);
			if (entity is IEvent)
				return ambience.Convert(entity as IEvent);
			if (entity is IField)
				return ambience.Convert(entity as IField);
			if (entity is IClass)
				return ambience.Convert(entity as IClass);
			// unknown entity:
			return entity.ToString();
		}

		public static string XmlDocumentationToText(string xmlDoc)
		{
			System.Diagnostics.Debug.WriteLine(xmlDoc);
			StringBuilder b = new StringBuilder();
			try {
				using (XmlTextReader reader = new XmlTextReader(new StringReader("<root>" + xmlDoc + "</root>"))) {
					reader.XmlResolver = null;
					while (reader.Read()) {
						switch (reader.NodeType) {
							case XmlNodeType.Text:
								b.Append(reader.Value);
								break;
							case XmlNodeType.Element:
								switch (reader.Name) {
									case "filterpriority":
										reader.Skip();
										break;
									case "returns":
										b.AppendLine();
										b.Append("Returns: ");
										break;
									case "param":
										b.AppendLine();
										b.Append(reader.GetAttribute("name") + ": ");
										break;
									case "remarks":
										b.AppendLine();
										b.Append("Remarks: ");
										break;
									case "see":
										if (reader.IsEmptyElement) {
											b.Append(reader.GetAttribute("cref"));
										} else {
											reader.MoveToContent();
											if (reader.HasValue) {
												b.Append(reader.Value);
											} else {
												b.Append(reader.GetAttribute("cref"));
											}
										}
										break;
								}
								break;
						}
					}
				}
				return b.ToString();
			} catch (XmlException) {
				return xmlDoc;
			}
		}
	}
}
