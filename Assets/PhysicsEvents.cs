using System;
using UnityEngine;

/// <summary>
/// حدثات فيزيائية ثابتة لنقل معلومات الاصطدام/الرش إلى أنظمة الجرافيكس.
/// هذا الملف لا يعتمد على أي مراجع رسومية ويُبقِي الطبقات منفصلة كما طُلب.
/// </summary>
public static class PhysicsEvents
{
    /// <summary>
    /// المفوض لحمل بيانات الاصطدام المفصلة لبقعة الطلاء.
    /// </summary>
    public delegate void PaintSplatterHandler(Vector3 impactPosition, Color paintColor, Vector3 impactVelocity, float viscosity);

    /// <summary>
    /// الحدث الاستاتيكي الذي يمكن الاشتراك به من قبل أنظمة الجرافيكس.
    /// </summary>
    public static PaintSplatterHandler OnPaintSplatterSplatted;

    /// <summary>
    /// حدث احتياطي للتماشي مع الواجهة الحالية.
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
