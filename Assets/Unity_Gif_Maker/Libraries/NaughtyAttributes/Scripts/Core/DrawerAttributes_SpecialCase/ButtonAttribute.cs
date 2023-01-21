using System;

namespace NaughtyAttributes
{
	public enum EButtonEnableMode
	{
		/// <summary>
		/// Button should be active always
		/// </summary>
		Always,
		/// <summary>
		/// Button should be active only in editor
		/// </summary>
		Editor,
		/// <summary>
		/// Button should be active only in playmode
		/// </summary>
		Playmode
	}

	public enum ButtonPlacement
	{
		Top,
		Bottom
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class ButtonAttribute : SpecialCaseDrawerAttribute
	{
		public string Text { get; private set; }
		public EButtonEnableMode SelectedEnableMode { get; private set; }		
		public ButtonPlacement Placement { get; private set; }
		public ButtonAttribute(string text = null, EButtonEnableMode enabledMode = EButtonEnableMode.Always, ButtonPlacement placement = ButtonPlacement.Bottom)
		{
			this.Text = text;
			this.SelectedEnableMode = enabledMode;
			this.Placement = placement;
		}
	}
}
