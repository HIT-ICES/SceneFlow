using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Book))]
public class AutoFlip : MonoBehaviour
{
    public int AnimationFramesCount = 40;
    public bool AutoStartFlip = true;
    public Book ControledBook;
    public float DelayBeforeStarting = 0;
    private bool isFlipping;
    public FlipMode Mode;
    public float PageFlipTime = 1;

    public float TimeBetweenPages = 1;

    // Use this for initialization
    private void Start()
    {
        if (!ControledBook)
            ControledBook = GetComponent<Book>();
        if (AutoStartFlip)
            StartFlipping();
        ControledBook.OnFlip.AddListener(PageFlipped);
    }

    private void PageFlipped()
    {
        isFlipping = false;
    }

    public void StartFlipping()
    {
        StartCoroutine(FlipToEnd());
    }

    public void FlipRightPage()
    {
        if (isFlipping) return;
        if (ControledBook.currentPage >= ControledBook.TotalPageCount) return;
        isFlipping = true;
        var frameTime = PageFlipTime / AnimationFramesCount;
        var xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
        var xl = (ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2 * 0.9f;
        //float h =  ControledBook.Height * 0.5f;
        var h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;
        var dx = xl * 2 / AnimationFramesCount;
        StartCoroutine(FlipRTL(xc, xl, h, frameTime, dx));
    }

    public void FlipLeftPage()
    {
        if (isFlipping) return;
        if (ControledBook.currentPage <= 0) return;
        isFlipping = true;
        var frameTime = PageFlipTime / AnimationFramesCount;
        var xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
        var xl = (ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2 * 0.9f;
        //float h =  ControledBook.Height * 0.5f;
        var h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;
        var dx = xl * 2 / AnimationFramesCount;
        StartCoroutine(FlipLTR(xc, xl, h, frameTime, dx));
    }

    private IEnumerator FlipToEnd()
    {
        yield return new WaitForSeconds(DelayBeforeStarting);
        var frameTime = PageFlipTime / AnimationFramesCount;
        var xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
        var xl = (ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2 * 0.9f;
        //float h =  ControledBook.Height * 0.5f;
        var h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;
        //y=-(h/(xl)^2)*(x-xc)^2          
        //               y         
        //               |          
        //               |          
        //               |          
        //_______________|_________________x         
        //              o|o             |
        //           o   |   o          |
        //         o     |     o        | h
        //        o      |      o       |
        //       o------xc-------o      -
        //               |<--xl-->
        //               |
        //               |
        var dx = xl * 2 / AnimationFramesCount;
        switch (Mode)
        {
            case FlipMode.RightToLeft:
                while (ControledBook.currentPage < ControledBook.TotalPageCount)
                {
                    StartCoroutine(FlipRTL(xc, xl, h, frameTime, dx));
                    yield return new WaitForSeconds(TimeBetweenPages);
                }

                break;
            case FlipMode.LeftToRight:
                while (ControledBook.currentPage > 0)
                {
                    StartCoroutine(FlipLTR(xc, xl, h, frameTime, dx));
                    yield return new WaitForSeconds(TimeBetweenPages);
                }

                break;
        }
    }

    private IEnumerator FlipRTL(float xc, float xl, float h, float frameTime, float dx)
    {
        var x = xc + xl;
        var y = -h / (xl * xl) * (x - xc) * (x - xc);

        ControledBook.DragRightPageToPoint(new Vector3(x, y, 0));
        for (var i = 0; i < AnimationFramesCount; i++)
        {
            y = -h / (xl * xl) * (x - xc) * (x - xc);
            ControledBook.UpdateBookRTLToPoint(new Vector3(x, y, 0));
            yield return new WaitForSeconds(frameTime);
            x -= dx;
        }

        ControledBook.ReleasePage();
    }

    private IEnumerator FlipLTR(float xc, float xl, float h, float frameTime, float dx)
    {
        var x = xc - xl;
        var y = -h / (xl * xl) * (x - xc) * (x - xc);
        ControledBook.DragLeftPageToPoint(new Vector3(x, y, 0));
        for (var i = 0; i < AnimationFramesCount; i++)
        {
            y = -h / (xl * xl) * (x - xc) * (x - xc);
            ControledBook.UpdateBookLTRToPoint(new Vector3(x, y, 0));
            yield return new WaitForSeconds(frameTime);
            x += dx;
        }

        ControledBook.ReleasePage();
    }
}