using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WpfAnimatedGif
{
    /// <summary>
    /// Provides a way to pause, resume or seek a GIF animation.
    /// </summary>
    public class ImageAnimationController : IDisposable
    {
        private static readonly DependencyPropertyDescriptor _sourceDescriptor;

        static ImageAnimationController()
        {
            _sourceDescriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof (Image));
        }

        private readonly Image _image;
        private readonly ObjectAnimationUsingKeyFrames _animation;
        private AnimationClock _clock;
        private ClockController _clockController;

        internal ImageAnimationController(Image image, ObjectAnimationUsingKeyFrames animation, bool autoStart)
        {
            _image = image;
            _animation = animation;
            _animation.Completed += AnimationCompleted;
            _clock = _animation.CreateClock();
            _clockController = _clock.Controller;
            _sourceDescriptor.AddValueChanged(image, ImageSourceChanged);

            // ReSharper disable once PossibleNullReferenceException
            _clockController.Pause();

            _image.ApplyAnimationClock(Image.SourceProperty, _clock);

            if (autoStart)
                _clockController.Resume();
        }

        void AnimationCompleted(object sender, EventArgs e)
        {
            _image.RaiseEvent(new System.Windows.RoutedEventArgs(ImageBehavior.AnimationCompletedEvent, _image));
        }

        private void ImageSourceChanged(object sender, EventArgs e)
        {
            OnCurrentFrameChanged();
        }

        /// <summary>
        /// Returns the number of frames in the image.
        /// </summary>
        public int FrameCount
        {
            get { return _animation.KeyFrames.Count; }
        }

        /// <summary>
        /// Returns the duration of the animation.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return _animation.Duration.HasTimeSpan
                  ? _animation.Duration.TimeSpan
                  : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether the animation is paused.
        /// </summary>
        public bool IsPaused
        {
            get { return _clock.IsPaused; }
        }

        /// <summary>
        /// Returns a value that indicates whether the animation is complete.
        /// </summary>
        public bool IsComplete
        {
            get { return _clock.CurrentState == ClockState.Filling; }
        }

        /// <summary>
        /// Seeks the animation to the specified frame index.
        /// </summary>
        /// <param name="index">The index of the frame to seek to</param>
        public void GotoFrame(int index)
        {
            var frame = _animation.KeyFrames[index];
            _clockController.Seek(frame.KeyTime.TimeSpan, TimeSeekOrigin.BeginTime);
        }

        /// <summary>
        /// Returns the current frame index.
        /// </summary>
        public int CurrentFrame
        {
            get
            {
                var time = _clock.CurrentTime;
                var frameAndIndex =
                    _animation.KeyFrames
                              .Cast<ObjectKeyFrame>()
                              .Select((f, i) => new { Time = f.KeyTime.TimeSpan, Index = i })
                              .FirstOrDefault(fi => fi.Time >= time);
                if (frameAndIndex != null)
                    return frameAndIndex.Index;
                return -1;
            }
        }

        /// <summary>
        /// Pauses the animation.
        /// </summary>
        public void Pause()
        {
            _clockController.Pause();
        }

        /// <summary>
        /// Starts or resumes the animation. If the animation is complete, it restarts from the beginning.
        /// </summary>
        public void Play()
        {
            _clockController.Resume();
        }

        /// <summary>
        /// Changes animation's duration by adjusting each <see cref="System.Windows.Media.Animation.KeyTime"/> 
        /// to an equal amount of time.
        /// </summary>
        /// <param name="newDuration">New duration for the entire animation.</param>
        public void ChangeDurationFlat(TimeSpan newDuration)
        {
            if (newDuration.TotalMilliseconds == Duration.TotalMilliseconds)
            {
                return;
            }

            bool isPaused = _clockController.Clock.IsPaused;
            if (!isPaused)
            {
                _clock.Controller.Pause();
            }

            var currentFrame = CurrentFrame;

            double sliceTime = newDuration.TotalMilliseconds / _animation.KeyFrames.Count;
            int frameCount = 0;

            foreach (var keyframeRaw in _animation.KeyFrames)
            {
                var keyFrame = keyframeRaw as DiscreteObjectKeyFrame;
                if (object.ReferenceEquals(null, keyFrame))
                {
                    continue;
                }

                keyFrame.KeyTime = new TimeSpan(0, 0, 0, 0, (int)((double)frameCount * sliceTime));
                frameCount++;
            }

            _animation.Duration = newDuration;

            // clock.Duration is derived from animation.Duration
            _clock = _animation.CreateClock();
            _clockController = _clock.Controller;

            _image.ApplyAnimationClock(Image.SourceProperty, _clock);

            if (currentFrame >= 0)
            {
                GotoFrame(currentFrame);
            }

            if (!isPaused)
            {
                _clock.Controller.Resume();
            }
        }

        /// <summary>
        /// <para>
        /// Changes animation's duration by multiplying each <see cref="System.Windows.Media.Animation.KeyTime"/> by a scale factor.
        /// </para>
        /// <para>
        /// This method is imprecise if called multiple times. <see cref="System.Windows.Media.Animation.KeyTime"/> should be recalculated
        /// based on the original timespans instead of scaling the timespans multiple times.
        /// </para>
        /// </summary>
        /// <param name="newDuration">New duration for the entire animation.</param>
        public void ChangeDurationScale(TimeSpan newDuration)
        {
            if (newDuration.TotalMilliseconds == Duration.TotalMilliseconds)
            {
                return;
            }

            bool isPaused = _clockController.Clock.IsPaused;
            if (!isPaused)
            {
                _clock.Controller.Pause();
            }

            var currentFrame = CurrentFrame;
            var scaleFactor = newDuration.TotalMilliseconds / _animation.Duration.TimeSpan.TotalMilliseconds;

            foreach (var keyframeRaw in _animation.KeyFrames)
            {
                var keyFrame = keyframeRaw as DiscreteObjectKeyFrame;
                if (object.ReferenceEquals(null, keyFrame))
                {
                    continue;
                }

                keyFrame.KeyTime = new TimeSpan(0, 0, 0, 0, (int)((double)keyFrame.KeyTime.TimeSpan.TotalMilliseconds * scaleFactor));
            }

            _animation.Duration = newDuration;

            // clock.Duration is derived from animation.Duration
            _clock = _animation.CreateClock();
            _clockController = _clock.Controller;

            _image.ApplyAnimationClock(Image.SourceProperty, _clock);

            if (currentFrame >= 0)
            {
                GotoFrame(currentFrame);
            }

            if (!isPaused)
            {
                _clock.Controller.Resume();
            }
        }

        /// <summary>
        /// Raised when the current frame changes.
        /// </summary>
        public event EventHandler CurrentFrameChanged;

        private void OnCurrentFrameChanged()
        {
            EventHandler handler = CurrentFrameChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Finalizes the current object.
        /// </summary>
        ~ImageAnimationController()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the current object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the current object
        /// </summary>
        /// <param name="disposing">true to dispose both managed an unmanaged resources, false to dispose only managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image.BeginAnimation(Image.SourceProperty, null);
                _animation.Completed -= AnimationCompleted;
                _sourceDescriptor.RemoveValueChanged(_image, ImageSourceChanged);
                _image.Source = null;
            }
        }
    }
}