// DrawingLine.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawingLine : MonoBehaviour
{
    public float pointSpacing = 0.1f;
    public PathChecker pathChecker;

    private LineRenderer lineRenderer;
    private List<Vector3> points = new List<Vector3>();
    private Vector3 lastAdded;
    private bool wasDrawing = false;

    // <<< این دو تابع جدید اضافه شده‌اند >>>
    void OnEnable()
    {
        // به سیگنال ریست سراسری گوش بده
        GameManager.OnStageRespawn += ResetLine;
    }

    void OnDisable()
    {
        // لغو ثبت‌نام برای جلوگیری از خطا
        GameManager.OnStageRespawn -= ResetLine;
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        ResetLine(); // برای اطمینان از پاک بودن خط در ابتدای بازی
    }

    // <<< این تابع کاملاً جدید است >>>
    public void ResetLine()
    {
        points.Clear();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
        wasDrawing = false;
        Debug.Log("DrawingLine has been reset.");
    }

    void Update()
    {
        // ... (بقیه کد Update شما بدون تغییر باقی می‌ماند) ...
        if (pathChecker == null || pathChecker.pen == null) return;
        if (pathChecker.isDrawing && !wasDrawing)
        {
            points.Clear();
            lineRenderer.positionCount = 0;
            Vector3 startPos = pathChecker.pen.position;
            startPos.z = 0;
            points.Add(startPos);
            lineRenderer.positionCount = 1;
            lineRenderer.SetPosition(0, startPos);
            lastAdded = startPos;
            wasDrawing = true;
        }
        else if (!pathChecker.isDrawing && wasDrawing)
        {
            wasDrawing = false;
        }
        if (pathChecker.isDrawing)
        {
            Vector3 penPos = pathChecker.pen.position;
            penPos.z = 0;
            if (Vector3.Distance(lastAdded, penPos) > pointSpacing)
            {
                points.Add(penPos);
                lineRenderer.positionCount = points.Count;
                lineRenderer.SetPosition(points.Count - 1, penPos);
                lastAdded = penPos;
            }
        }
    }
}