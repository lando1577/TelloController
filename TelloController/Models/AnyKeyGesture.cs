using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TelloController.Models
{
    public class AnyKeyGesture : InputGesture
    {
        public AnyKeyGesture(Key key)
        {
            Key = key;
        }

        public Key Key
        {
            get;
            protected set;
        }



        ///
        /// When overridden in a derived class, determines whether the specified matches the input associated with the specified object.
        ///
        /// The target of the command.
        /// The input event data to compare this gesture to.
        ///
        /// true if the gesture matches the input; otherwise, false.
        ///
        public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
        {
            KeyEventArgs args = inputEventArgs as KeyEventArgs;
            if (args == null) return false;
            return (Key == args.Key);
        }

    }


}
