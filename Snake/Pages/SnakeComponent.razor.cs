using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Snake;
using Blazor.Extensions;
using Blazor.Extensions.Canvas;
using Blazor.Extensions.Canvas.Canvas2D;
using System.Timers;
using static System.Formats.Asn1.AsnWriter;

namespace Snake.Pages
{
    public partial class SnakeComponent
    {
        private Canvas2DContext _context;
        protected BECanvasComponent _canvasReference;
        [Parameter]
        public int Rows { get; set; } = 20;
        [Parameter]
        public int Columns { get; set; } = 20;
        public int Score { get; set; } = 0;
        public System.Timers.Timer Timer { get; private set; }

        private Position _headPosition = new Position(1, 1);
        private Position _applePosition;
        private Position _direction = new Position(0, 1);
        private List<Tail> _tails = new List<Tail>();
        private int _snakeLength = 7;
        //private float[,] _costsMap;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        //    _costsMap = new float[Rows, Columns];
            if (Timer == null)
                Timer = new System.Timers.Timer(100);

            Timer.Elapsed -= async (_, _) => await InvokeAsync(() => StateHasChanged());
            Timer.Elapsed += async (_, _) => await InvokeAsync(() => StateHasChanged());
            Timer.Start();
            SetApplePosition();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _context = await _canvasReference.CreateCanvas2DAsync();
            await _context.ClearRectAsync(0, 0, _canvasReference.Width, _canvasReference.Height);
            Move();
            DrawSnakeAsync();
            DrawTails();
            await DrawAppleAsync();
            FollowHead();
        }
        public void Move()
        {
            SelectDirection();
            _headPosition.x = Math.Clamp(_headPosition.x + _direction.x, 1, _canvasReference.Width);
            _headPosition.y = Math.Clamp(_headPosition.y + _direction.y, 1, _canvasReference.Height);
            CheckCollision();
        }

        private void CheckCollision()
        {
            if (_headPosition.Equals(_applePosition)) EatApple();

            for (int i = 0; i < _tails.Count; i++)
            {
                Tail tail = _tails[i];

                if (tail.Position.Equals(_applePosition)) EatApple();
                if (_headPosition.Equals(tail.Position)) GameOver();
            }
        }

        private void FollowHead()
        {
            if (_tails.Count > 0)
            {
                Tail last = _tails.Last();
                last.Position = _headPosition;
                _tails.Insert(0, last);
                _tails.RemoveAt(_tails.Count - 1);
            }
        }

        #region Drawing methods
        private void DrawTails()
        {
            foreach (var tail in _tails)
                DrawSnakeAtPositionAsync(tail.Position);
        }

        private async Task DrawAppleAsync()
        {
            await _context.BeginPathAsync();
            var pixelPos = PositionToPixels(_applePosition);
            await _context.ArcAsync(pixelPos.x, pixelPos.y, _snakeLength, 0, 360);
            await _context.FillAsync();
            await _context.StrokeAsync();
        }

        private async void DrawSnakeAsync()
        {
            await DrawSnakeAtPositionAsync(_headPosition);
        }
        public async Task DrawSnakeAtPositionAsync(Position position)
        {
            await _context.BeginPathAsync();
            var pixelPos = PositionToPixels(position);
            await _context.ArcAsync(pixelPos.x, pixelPos.y, _snakeLength, 0, 2 * Math.PI);
            await _context.StrokeAsync();
        }
        #endregion

        private void GameOver()
        {
            _tails = new List<Tail>();
            _headPosition = new Position(0, 0);
            SetApplePosition();
            Score = 0;
        }

        #region Auto movement
        public void SelectDirection()
        {
            if (Math.Abs(_applePosition.x - _headPosition.x) > 0)
            {
                SelectDirectionX();
                /*if (CheckCollitionDirection(_direction))
                    SelectDirectionY();*/
            }
            else if (Math.Abs(_applePosition.y - _headPosition.y) > 0)
            {
                SelectDirectionY();
               /*if(CheckCollitionDirection(_direction))
                    SelectDirectionX();*/
            }
        }

        public void SelectDirectionX()
        {
            var tmpDirection = Math.Clamp(_applePosition.x - _headPosition.x, -1, 1);
            if (!IsOpositeDirection(_direction.x, tmpDirection)) // If they were, snake will go bump into its own tail
            {
                _direction.x = tmpDirection;
                _direction.y = 0;
            } else
            {
                _direction.x = 0;
                _direction.y = 1;
            }
            
        }

        public void SelectDirectionY()
        {
            var tmpDirection = Math.Clamp(_applePosition.y - _headPosition.y, -1, 1);
            if (!IsOpositeDirection(_direction.y, tmpDirection)) // If they were, snake will go bump into its own tail
            {
                _direction.x = 0;
                _direction.y = tmpDirection;
            }
            else
            {
                _direction.x = 1;
                _direction.y = 0;
            }
        }

        // This method was intended to avoid tail
        private bool CheckCollitionDirection(Position direction)
        {
            if (direction.x == 0) // Check collitions in vertical
            {
                for (int i = (int)_headPosition.y; i < _applePosition.y; i += (int)direction.y)
                    foreach (var tail in _tails)
                        if (tail.Position.y == i)
                            return true;
                return false;
            }

            // Check collitions in horizontal
            for (int i = (int)_headPosition.x; i < _applePosition.x; i += (int)direction.x)
                foreach (var tail in _tails)
                    if (tail.Position.x == i)
                        return true;
            return false;
        }
        #endregion

        #region Manual movement handler
        public void KeyboardHandler(KeyboardEventArgs e)
        {
            switch (e.Code)
            {
                case "ArrowUp":
                    ChangeDirection(Position.Up); break;
                case "ArrowDown":
                    ChangeDirection(Position.Down); break;
                case "ArrowLeft":
                    ChangeDirection(Position.Left); break;
                case "ArrowRight":
                    ChangeDirection(Position.Right); break;
            }
        }
        public void ChangeDirection(Position dir)
        {
            _direction = dir;
        }
        #endregion

        public void EatApple()
        {
            _tails.Add(new Tail(GetNextPosition(_headPosition)));
            Score++;
            SetApplePosition();
        }

        public void SetApplePosition()
        {
            _applePosition = Position.GetRandomPosition(Rows, Columns);
        }

        public Position GetNextPosition(Position position)
        {
            position.x += _direction.x;
            position.y += _direction.y;

            return position;
        }

        public Position PositionToPixels(Position position)
        {
            position.x *= _snakeLength * 2; // every square should be double of snake radious
            position.y *= _snakeLength * 2;

            return position;
        }

        public bool IsOpositeDirection(float x1, float x2)
        {
            return x1 == -x2;
        }
    }
}