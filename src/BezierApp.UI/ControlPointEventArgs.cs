using System;

namespace BezierApp.UI
{
    /// <summary>Datos de un evento que se refiere a un punto de control concreto.</summary>
    public sealed class ControlPointEventArgs : EventArgs
    {
        public ControlPointEventArgs(int index)
        {
            Index = index;
        }

        /// <summary>Posición del punto dentro de la lista de puntos de control.</summary>
        public int Index { get; private set; }
    }
}
