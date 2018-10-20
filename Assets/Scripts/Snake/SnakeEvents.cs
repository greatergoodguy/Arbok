using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SnakeEvents {
    Dictionary<int, SnakeCompoundEvent> dict = new Dictionary<int, SnakeCompoundEvent>();

    public void AddOrReplaceAtTick(int tick, SnakeEvent snakeEvent) {
        if (dict.ContainsKey(tick) == false) {
            dict[tick] = new SnakeCompoundEvent();
        }
        dict[tick].AddOrReplaceEvent(snakeEvent);
    }

    public bool HasEventAtTick(int tick) {
        return dict.ContainsKey(tick);
    }

    public void ExecuteEventsAtTickIfAny(int tick, Snake snake) {
        if (dict.ContainsKey(tick)) {
            dict[tick].Execute(snake);
        }
    }

    public void ReverseEventsAtTickIfAny(int tick, Snake snake) {
        if (dict.ContainsKey(tick)) {
            dict[tick].Reverse(snake);
        }
    }
}

public class SnakeCompoundEvent : Dictionary<Type, SnakeEvent> {
    Dictionary<Type, SnakeEvent> events = new Dictionary<Type, SnakeEvent>();

    public void AddOrReplaceEvent(SnakeEvent snakeEvent) {
        events[snakeEvent.GetType()] = snakeEvent;
    }

    public void Execute(Snake snake) {
        foreach (var kvp in events) {
            kvp.Value.Execute(snake);
        }
    }

    public void Reverse(Snake snake) {
        foreach (var kvp in events.Reverse()) {
            kvp.Value.Reverse(snake);
        }
    }
}

public interface SnakeEvent {
    void Execute(Snake snake);
    void Reverse(Snake snake);
}

class SnakeMoveEvent : SnakeEvent {
    Vector3 oldTailPosition;

    public void Execute(Snake snake) {
        Vector3 newPosition = snake.head.transform.position + snake.currentDirection.GetMoveVector();

        if (AboutToCollideWithSelf(snake, newPosition)) {
            snake.Die();
        }

        if (snake.links.Count > 0) {
            var oldTail = snake.links.Last();
            snake.links.RemoveAt(snake.links.Count - 1);
            snake.links.Insert(0, oldTail);
            oldTailPosition = oldTail.transform.position;
            oldTail.transform.position = snake.head.transform.position;
        }

        snake.head.transform.rotation = snake.currentDirection.GetHeadRotation();
        snake.head.transform.position = newPosition;
    }

    private bool AboutToCollideWithSelf(Snake snake, Vector3 newPosition) {
        return snake.links.Any(x => x.transform.position == newPosition);
    }

    public void Reverse(Snake snake) {
        Vector3 oldPosition = snake.head.transform.position - snake.currentDirection.GetMoveVector();

        snake.head.transform.position = oldPosition;
        snake.head.transform.rotation = snake.currentDirection.GetHeadRotation();

        if (snake.links.Count > 0) {
            var newTail = snake.links[0];
            snake.links.RemoveAt(0);
            snake.links.Add(newTail);
            newTail.transform.position = oldTailPosition;
        }
    }
}

public class SnakeChangeDirectionEvent : SnakeEvent {
    Direction previousDirection = DummyDirection.I;
    Direction newDirection;

    public SnakeChangeDirectionEvent(Direction newDirection) {
        this.newDirection = newDirection;
    }

    public void Execute(Snake snake) {
        previousDirection = snake.currentDirection;

        if (invalidTurn(snake.currentDirection, newDirection)) return;

        snake.currentDirection = newDirection;
    }

    public void Reverse(Snake snake) {
        snake.currentDirection = previousDirection;
    }

    private static bool invalidTurn(Direction currentDir, Direction newDir) {
        if ((newDir == Down.I && currentDir == Up.I) ||
            (newDir == Left.I && currentDir == Right.I) ||
            (newDir == Up.I && currentDir == Down.I) ||
            (newDir == Right.I && currentDir == Left.I)) {

            return true;
        } else {
            return false;
        }
    }
}

public class SnakeEatAppleEvent : SnakeEvent {
    GameObject apple;

    public SnakeEatAppleEvent(GameObject apple) {
        this.apple = apple;
    }

    public void Execute(Snake snake) {
        apple.SetActive(false);
        var newTail = GameObject.Instantiate(snake.snakeTailPrefab, snake.transform);
        snake.links.Add(newTail);
    }

    public void Reverse(Snake snake) {
        var firstTailLink = snake.links.Last();
        snake.links.Remove(firstTailLink);
        GameObject.Destroy(firstTailLink);
        apple.SetActive(true);
    }
}