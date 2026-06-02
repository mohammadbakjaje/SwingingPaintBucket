using System;
using UnityEngine;

/// <summary>
/// حدثات فيزيائية ثابتة لنقل معلومات الاصطدام/الرش إلى أنظمة الجرافيكس.
/// هذا الملف لا يعتمد على أي مراجع رسومية ويُبقِي الطبقات منفصلة كما طُلب.
/// </summary>
public static class PhysicsEvents
{
    /// <summary>
    /// حدث يُبث عند حدوث رشّة طلاء (Splatter).
    /// الوسائط: (position, color, speed, viscosity)
    /// </summary>
    public static Action<Vector3, Color, float, float> OnPaintSplatter;

    /// <summary>
    /// طريقة آمنة لإطلاق حدث رشّة الطلاء من المحرك الفيزيائي.
    /// </summary>
    public static void TriggerPaintSplatter(Vector3 position, Color color, float speed, float viscosity)
    {
        OnPaintSplatter?.Invoke(position, color, speed, viscosity);
    }
}
