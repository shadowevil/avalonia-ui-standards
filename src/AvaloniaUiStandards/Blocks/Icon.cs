using Avalonia.Controls;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// An icon from <see cref="Icons"/> (or any geometry drawn in a 256 x 256 design box). It is a
/// <see cref="PathIcon"/>, so it goes wherever one does. Its theme keeps the whole design box, so
/// every icon keeps its proportions and position within it. It is 20 px at Normal density and
/// 16 px at Compact, or any size set through <c>Width</c> and <c>Height</c> (24 for larger use).
/// Its colour is the inherited <c>Foreground</c>, the text colour of the control it sits in.
/// </summary>
public class Icon : PathIcon
{
}
