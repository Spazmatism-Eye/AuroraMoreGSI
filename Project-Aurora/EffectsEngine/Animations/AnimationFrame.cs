﻿using System.Drawing;

namespace Aurora.EffectsEngine.Animations
{
    public class AnimationFrame
    {
        internal Color _color;
        internal RectangleF _dimension;
        internal int _width;
        internal float _duration;
        internal Pen _pen = null;
        internal Brush _brush = null;
        internal bool _invalidated = true;
        internal bool _isIgnored = false;

        public Color Color { get { return _color; } }
        public RectangleF Dimension { get { return _dimension; } }
        public int Width { get { return _width; } }
        public float Duration { get { return _duration; } }
        public bool IsIgnored { get { return _isIgnored; } }

        public AnimationFrame()
        {
            _color = new Color();
            _dimension = new RectangleF();
            _width = 1;
            _duration = 0.0f;
        }

        public AnimationFrame(Rectangle dimension, Color color, int width = 1, float duration = 0.0f)
        {
            _color = color;
            _dimension = dimension;
            _width = width;
            _duration = duration;
        }

        public AnimationFrame(RectangleF dimension, Color color, int width = 1, float duration = 0.0f)
        {
            _color = color;
            _dimension = dimension;
            _width = width;
            _duration = duration;
        }

        public AnimationFrame SetColor(Color color)
        {
            _color = color;
            _invalidated = true;

            return this;
        }

        public AnimationFrame SetDimension(RectangleF dimension)
        {
            _dimension = dimension;
            _invalidated = true;

            return this;
        }

        public AnimationFrame SetWidth(int width)
        {
            _width = width;
            _invalidated = true;

            return this;
        }

        public AnimationFrame SetDuration(float duration)
        {
            _duration = duration;

            return this;
        }

        public AnimationFrame SetIgnore(bool state)
        {
            _isIgnored = state;

            return this;
        }

        public virtual void Draw(Graphics g, float scale = 1.0f) { }
        public virtual AnimationFrame BlendWith(AnimationFrame otherAnim, double amount)
        {

            RectangleF newrect = new RectangleF((float)(_dimension.X * (1.0 - amount) + otherAnim._dimension.X * (amount)),
                (float)(_dimension.Y * (1.0 - amount) + otherAnim._dimension.Y * (amount)),
                (float)(_dimension.Width * (1.0 - amount) + otherAnim._dimension.Width * (amount)),
                (float)(_dimension.Height * (1.0 - amount) + otherAnim._dimension.Height * (amount))
                );


            AnimationFrame newframe = new AnimationFrame();
            newframe._dimension = newrect;
            newframe._color = Utils.ColorUtils.BlendColors(_color, otherAnim._color, amount);

            return newframe;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AnimationFrame)obj);
        }

        public bool Equals(AnimationFrame p)
        {
            return _color.Equals(p._color) &&
                _dimension.Equals(p._dimension) &&
                _width.Equals(p._width) &&
                _duration.Equals(p._duration);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + _color.GetHashCode();
                hash = hash * 23 + _dimension.GetHashCode();
                hash = hash * 23 + _width.GetHashCode();
                hash = hash * 23 + _duration.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return "AnimationFrame [ Color: " + _color.ToString() + " Dimensions: " + _dimension.ToString() + " Width: " + _width + "]";
        }
    }
}
