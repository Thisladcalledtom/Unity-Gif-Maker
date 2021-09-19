using System;

namespace NaughtyAttributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class FoldoutAttribute : MetaAttribute, IGroupAttribute
	{
		public string Name { get; private set; }

		public FoldoutAttribute(string name)
		{
			Name = name;
		}
	}
}
