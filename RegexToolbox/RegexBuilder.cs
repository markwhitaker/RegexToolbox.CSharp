using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RegexToolbox
{
    /// <summary>
    /// Class to build regular expressions in a more human-readable way using a fluent API.
    /// 
    /// To use, chain method calls representing the elements you want to match, and finish with
    /// <see cref="BuildRegex"/> to build the Regex. Example:
    /// 
    ///    Regex regex = new RegexBuilder()
    ///                     .Text("cat")
    ///                     .EndOfString()
    ///                  .BuildRegex();
    /// 
    /// </summary>
    public class RegexBuilder
    {
        protected readonly StringBuilder StringBuilder;
        protected readonly RegexBuilder Parent;

        #region Constructors

        public RegexBuilder()
        {
            StringBuilder = new StringBuilder();
            Parent = null;
        }

        protected RegexBuilder(RegexBuilder parent)
        {
            Parent = parent;
            StringBuilder = parent.StringBuilder;
        }

        #endregion
        
        #region Build method

        /// <summary>
        /// Build and return a Regex object from the current builder state.
        /// After calling this the builder is cleared and ready to re-use.
        /// </summary>
        /// <param name="options">Any number of regex options to apply to the regex</param>
        /// <returns>Regex as built</returns>
        public virtual Regex BuildRegex(params RegexOptions[] options)
        {
            RegexOptions allOptions = options.Aggregate(RegexOptions.None, (current, option) => current | option);

            if (allOptions.HasFlag(RegexOptions.Multiline) && allOptions.HasFlag(RegexOptions.Singleline))
            {
                throw new RegexBuilderException("Cannot specify both single line and multi-line options", StringBuilder);
            }

            var regex = new Regex(StringBuilder.ToString(), allOptions);
            StringBuilder.Clear();
            return regex;
        }

        #endregion

        #region Character matches

        /// <summary>
        /// Add text to the regex. Any regex special characters will be escaped as necessary
        /// so there's no need to do that yourself.
        /// </summary>
        /// <example>
        /// "Hello (world)" will be converted to "Hello \(world\)" so the brackets are treated
        /// as normal, human-readable brackets, not regex grouping brackets.
        /// It WILL match the string literal "Hello (world)".
        /// It WILL NOT match the string literal "Hello world".
        /// </example>
        /// <param name="text">Text to add</param>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder Text(string text, RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(MakeSafeForRegex(text));
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add literal regex text to the regex. Regex special characters will NOT be escaped.
        /// Only call this if you're comfortable with regex syntax.
        /// </summary>
        /// <example>
        /// "Hello (world)" will be left as "Hello (world)", meaning that when the regex is built
        /// the brackets will be treated as regex grouping brackets rather than normal, human-readable
        /// brackets.
        /// It WILL match the string literal "Hello world" (and capture the word "world" as a group).
        /// It WILL NOT match the string literal "Hello (world)".
        /// </example>
        /// <param name="text">regex text to add</param>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder RegexText(string text, RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(text);
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any character.
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder AnyCharacter(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(".");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any single whitespace character.
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder Whitespace(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(@"\s");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any single non-whitespace character.
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder NonWhitespace(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(@"\S");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any single decimal digit (0-9).
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder Digit(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(@"\d");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any character that is not a decimal digit (0-9).
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder NonDigit(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(@"\D");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any letter in the Roman alphabet (a-z, A-Z)
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder Letter(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append("[a-zA-Z]");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any character that is not a letter in the Roman alphabet (a-z, A-Z)
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder NonLetter(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append("[^a-zA-Z]");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any uppercase letter in the Roman alphabet (A-Z).
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder UppercaseLetter(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append("[A-Z]");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any lowercase letter in the Roman alphabet (a-z)
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder LowercaseLetter(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append("[a-z]");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any letter in the Roman alphabet or decimal digit (a-z, A-Z, 0-9)
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder LetterOrDigit(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append("[a-zA-Z0-9]");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any character that is not letter in the Roman alphabet or a decimal digit (a-z, A-Z, 0-9)
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder NonLetterOrDigit(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append("[^a-zA-Z0-9]");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any Roman alphabet letter, decimal digit, or underscore (a-z, A-Z, 0-9, _)
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder WordCharacter(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(@"\w");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element to match any character that is not a Roman alphabet letter, decimal digit, or underscore (a-z, A-Z, 0-9, _)
        /// </summary>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder NonWordCharacter(RegexQuantifier quantifier = null)
        {
            StringBuilder.Append(@"\W");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element (a character class) to match any of the characters provided.
        /// </summary>
        /// <param name="characters">String containing all characters to include in the character class</param>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder AnyCharacterFrom(string characters, RegexQuantifier quantifier = null)
        {
            // Build a character class, remembering to escape any ] character if passed in
            StringBuilder.Append("[" + MakeSafeForCharacterClass(characters) + "]");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add an element (a character class) to match any character except those provided.
        /// </summary>
        /// <param name="characters">String containing all characters to exclude from the character class</param>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder AnyCharacterExcept(string characters, RegexQuantifier quantifier = null)
        {
            // Build a character class, remembering to escape any ] character if passed in
            StringBuilder.Append("[^" + MakeSafeForCharacterClass(characters) + "]");
            AddQuantifier(quantifier);
            return this;
        }

        /// <summary>
        /// Add a group of alternatives, to match any of the strings provided
        /// </summary>
        /// <param name="strings">A number of strings, any one of which will be matched</param>
        /// <param name="quantifier">Quantifier to apply to this element</param>
        public RegexBuilder AnyOf(IEnumerable<string> strings, RegexQuantifier quantifier = null)
        {
            if (strings == null)
            {
                return this;
            }

            var stringsList = strings.ToList();
            if (!stringsList.Any())
            {
                return null;
            }

            if (stringsList.Count == 1)
            {
                StringBuilder.Append(MakeSafeForRegex(stringsList[0]));
                AddQuantifier(quantifier);
                return this;
            }

            return StartGroup()
                .RegexText(string.Join("|", stringsList.Select(MakeSafeForRegex)))
                .EndGroup(quantifier);
        }

        #endregion

        #region Anchors (zero-width assertions)

        /// <summary>
        /// Add a zero-width anchor element to match the start of the string
        /// </summary>
        public RegexBuilder StartOfString()
        {
            StringBuilder.Append("^");
            return this;
        }

        /// <summary>
        /// Add a zero-width anchor element to match the end of the string
        /// </summary>
        public RegexBuilder EndOfString()
        {
            StringBuilder.Append("$");
            return this;
        }

        /// <summary>
        /// Add a zero-width anchor element to match the boundary between an alphanumeric/underscore character
        /// and either a non-alphanumeric, non-underscore character or the start/end of the string.
        /// </summary>
        public RegexBuilder WordBoundary()
        {
            StringBuilder.Append(@"\b");
            return this;
        }

        #endregion

        #region Grouping

        /// <summary>
        /// Start a capture group. Capture groups have two purposes: they group part of the expression so
        /// it can have quantifiers applied to it, and they capture the results of each group match and
        /// allow you to access them afterwards using Match.Groups.
        /// 
        /// If you don't want to capture the group match, use <see cref="StartNonCapturingGroup"/>.
        /// 
        /// Note: all groups must be ended with <see cref="EndGroup"/> before calling <see cref="BuildRegex"/>.
        /// </summary>
        public RegexBuilder StartGroup()
        {
            StringBuilder.Append("(");
            return new RegexGroupBuilder(this);
        }

        /// <summary>
        /// Start a non-capturing group. Non-capturing groups group part of the expression so
        /// it can have quanitifiers applied to it, but do not capture the results of each group match, meaning
        /// you can't access them afterwards using Match.Groups.
        /// 
        /// If you want to capture group results, use <see cref="StartGroup"/> or <see cref="StartNamedGroup"/>.
        /// 
        /// Note: all groups must be ended with <see cref="EndGroup"/> before calling <see cref="BuildRegex"/>.
        /// </summary>
        public RegexBuilder StartNonCapturingGroup()
        {
            StringBuilder.Append("(?:");
            return new RegexGroupBuilder(this);
        }

        /// <summary>
        /// Start a named capture group. Capture groups have two purposes: they group part of the expression so
        /// it can have quanitifiers applied to it, and they capture the results of each group match and
        /// allow you to access them afterwards using Match.Groups. Named capture groups can be accessed by
        /// indexing into Match.Groups with the assigned name as well as a numerical index.
        /// 
        /// If you don't want to capture the group match, use <see cref="StartNonCapturingGroup"/>.
        /// 
        /// Note: all groups must be ended with <see cref="EndGroup"/> before calling <see cref="BuildRegex"/>.
        /// </summary>
        public RegexBuilder StartNamedGroup(string name)
        {
            StringBuilder.Append("(?<" + name + ">");
            return new RegexGroupBuilder(this);
        }

        /// <summary>
        /// Start a zero-width, positive look-ahead group.
        /// </summary>
        public RegexBuilder StartPositiveLookAheadGroup()
        {
            StringBuilder.Append("(?=");
            return new RegexGroupBuilder(this);
        }

		/// <summary>
		/// Start a zero-width, negative look-ahead group.
		/// </summary>
		public RegexBuilder StartNegativeLookAheadGroup()
		{
			StringBuilder.Append("(?!");
			return new RegexGroupBuilder(this);
		}

		/// <summary>
		/// Start a zero-width, positive look-behind group.
		/// </summary>
		public RegexBuilder StartPositiveLookBehindGroup()
		{
			StringBuilder.Append("(?<=");
			return new RegexGroupBuilder(this);
		}

		/// <summary>
		/// Start a zero-width, negative look-behind group.
		/// </summary>
		public RegexBuilder StartNegativeLookBehindGroup()
		{
			StringBuilder.Append("(?<!");
			return new RegexGroupBuilder(this);
		}

		/// <summary>
		/// Add a zero-width element to start a capture group. Capture groups remember subsets of the
		/// matched string and allow you to access them afterwards using Match.Groups.
		/// 
		/// Note: all groups must be ended with <see cref="EndGroup"/> before calling <see cref="BuildRegex"/>.
		/// </summary>
		/// <param name="quantifier">Quantifier to apply to this group</param>
		public virtual RegexBuilder EndGroup(RegexQuantifier quantifier = null)
        {
            throw new RegexBuilderException("Cannot call Endgroup() until a group has been started with StartGroup()", StringBuilder);
        }

        #endregion

        #region Private methods

        private void AddQuantifier(RegexQuantifier quantifier)
        {
            if (quantifier != null)
            {
                StringBuilder.Append(quantifier);
            }
        }

        private string MakeSafeForCharacterClass(string s)
        {
            // Replace ] with \]
            var result = s.Replace("]", @"\]");

            // replace ^ with \^ if it occurs at the start of the string
            if (result.StartsWith("^"))
            {
                result = @"\" + result;
            }

            return result;
        }

        private static string MakeSafeForRegex(string s)
        {
            var result = s
                // Make sure this always comes first!
                .Replace(@"\", @"\\")
                .Replace("?", @"\?")
                .Replace(".", @"\.")
                .Replace("+", @"\+")
                .Replace("*", @"\*")
                .Replace("^", @"\^")
                .Replace("$", @"\$")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace("[", @"\[")
                .Replace("]", @"\]")
                .Replace("{", @"\{")
                .Replace("}", @"\}")
                .Replace("|", @"\|");

            return result;
        }
        #endregion

        #region Member classes

        /// <summary>
        /// Derived class to represent a group within a regex
        /// </summary>
        public sealed class RegexGroupBuilder : RegexBuilder
        {
            public RegexGroupBuilder(RegexBuilder parent) : base(parent)
            {
            }

            public override RegexBuilder EndGroup(RegexQuantifier quantifier = null)
            {
                StringBuilder.Append(")");
                AddQuantifier(quantifier);
                return Parent;
            }

            public override Regex BuildRegex(params RegexOptions[] options)
            {
                throw new RegexBuilderException("At least one group is still open", StringBuilder);
            }
        }

        #endregion
    }
}
