using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Array = Godot.Collections.Array;

public static class Extensions
{
    /// <summary>
    /// Get child node by its class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static Node GetNodeOfType<T> (this Node node) where T : Node
    {
        //return node.FindNode(typeof(T).Name);
        return node.GetChildren().OfType<T>().FirstOrDefault();
    }

    public static Node RootNode(this Node node) => node.GetTree().Root;

    /// <summary>
    /// Get global node of type. (Instance name MUST be same as class' name)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static Node GetGlobal<T>(this Node node) => node.GetNode("/root/" + typeof(T).Name);

    //Directions
    public static Vector3 TranformForward(this Spatial spatial) => (spatial.ToGlobal(Vector3.Forward) - spatial.Translation).Normalized();
    public static Vector3 TranformRight(this Spatial spatial) => (spatial.ToGlobal(Vector3.Right) - spatial.Translation).Normalized();
    public static Vector3 TransformUp(this Spatial spatial) => (spatial.ToGlobal(Vector3.Up) - spatial.Translation).Normalized();

    //Math
    /// <summary>
    /// Simplification of LinearInterpolate.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    public static Vector3 Lerp(Vector3 from, Vector3 to, float time) => from.LinearInterpolate(to, time);

    /// <summary>
    /// Rotated(axis, andle) with automatically normalized axis.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="axis"></param>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector3 RotatedSafe(this Vector3 v, Vector3 axis, float angle) => axis == Vector3.Zero ? v : v.Rotated(axis.Normalized(), angle);

    public static float RandomRange(float min, float max) => GD.Randf() * (max - min) + min;
    public static int RandomRange(int min, int max) => (int)(GD.Randi() % max + min);

    //Raycast stuff
    public static Godot.Object GetCollider(this Godot.Collections.Dictionary result) => result.Contains("collider") ? (Godot.Object)result["collider"] : null;
    public static Vector3 GetPoint(this Godot.Collections.Dictionary result) => result.Contains("position") ? (Vector3)result["position"] : Vector3.Zero;
    public static Vector3 GetNormal(this Godot.Collections.Dictionary result) => result.Contains("normal") ? (Vector3)result["normal"] : Vector3.Zero;
    public static RID GetRID(this Godot.Collections.Dictionary result) => result.Contains("RID") ? (RID)result["RID"] : null;
    public static int GetShape(this Godot.Collections.Dictionary result) => result.Contains("shape") ? (int)result["shape"] : 0;

    //Sound
    /// <summary>
    /// Play one shot sound with auto instanced AudioStreamPlayer for polyphony. Temp instance gets removed automatically.
    /// </summary>
    /// <param name="audio"></param>
    /// <param name="sound"></param>
    public static async void PlaySoundPoly(AudioStreamPlayer3D audio, AudioStream sound)
    {
        if (!audio.Playing)
        {
            audio.Stream = sound;
            audio.Play();
        }
        else
        {
            AudioStreamPlayer3D player = (AudioStreamPlayer3D)audio.Duplicate((int)Node.DuplicateFlags.UseInstancing);
            audio.AddChild(player);
            player.Stream = sound;
            player.Play();
            await player.ToSignal(player, "finished");
            player.QueueFree();
        }
    }

    /// <summary>
    /// Play one shot sound with auto instanced AudioStreamPlayer for polyphony. Temp instance gets removed automatically.
    /// </summary>
    /// <param name="audio"></param>
    /// <param name="sound"></param>
    public static async void PlaySoundPoly(AudioStreamPlayer audio, AudioStream sound)
    {
        if (!audio.Playing)
        {
            audio.Stream = sound;
            audio.Play();
        }
        else
        {
            AudioStreamPlayer player = (AudioStreamPlayer)audio.Duplicate((int)Node.DuplicateFlags.UseInstancing);
            audio.AddChild(player);
            player.Stream = sound;
            player.Play();
            await player.ToSignal(player, "finished");
            player.QueueFree();
        }
    }

    //Animation
    public const string aParameters = "parameters";

    public static string AnimOneShotActivePath(string node) => $"{aParameters}/{node}/active";
    public static bool GetOneShotActive(this AnimationTree a, string node) => (bool)a.Get(AnimOneShotActivePath(node));
    public static void SetOneShotActive(this AnimationTree a, string node, bool b) => a.Set(AnimOneShotActivePath(node), b);
    public static string AnimTimeScalePath(string node) => $"{aParameters}/{node}/scale";
    public static void SetTimeScale(this AnimationTree a, string node, float speed) => a.Set(AnimTimeScalePath(node), speed);
    public static string AnimBlendPath(string node) => $"{aParameters}/{node}/blend_amount";
    public static string AnimSeekPosPath(string node) => $"{aParameters}/{node}/seek_position";
    public static void SetSeek(this AnimationTree a, string node, float pos = 0) => a.Set(AnimSeekPosPath(node), pos);
    public static string AnimPlaybackPath(string node = "") => $"{aParameters}/{(node != "" ? node + "/" : "")}playback";

    //physics
    /// <summary>
    /// Cast thick raycast, in shape of capsule.
    /// </summary>
    /// <param name="spatial"></param>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="radius"></param>
    /// <param name="distance"></param>
    /// <param name="mask"></param>
    /// <param name="exclude"></param>
    /// <returns></returns>
    public static Array CapsuleCast(this Spatial spatial, Vector3 origin, Vector3 direction, float radius, float distance, uint mask = uint.MaxValue, Array exclude = null)
    {
        var space_state = spatial.GetWorld().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters();
        var shape = new CapsuleShape();
        shape.Radius = radius;
        shape.Height = distance;
        query.ShapeRid = shape.GetRid();
        query.Transform = query.Transform.Translated(origin + (direction * distance) / 2);
        query.Transform = query.Transform.LookingAt(origin + direction, Vector3.Forward);
        query.Exclude = exclude;
        query.CollisionMask = mask;
        var results = space_state.IntersectShape(query, 10);
        return results;
    }

    //Experimental
    /// <summary>
    /// Convert method name to GDScript format (snake_case), useful in cases like CallDeferered.
    /// Use nameof(function) as string when calling GD functions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string ToGDName(this string funcName)
    {
        string s = funcName;

        var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

        return r.Replace(s, "_").ToLower();
    }
}
