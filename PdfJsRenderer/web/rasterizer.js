﻿var renderer = (function () {
    var self = this;
    var _dpi;

    self.start = function (filePath, dpi) {
        //PDFJS.disableWorker = true;
        //alert(dpi);
        _dpi = dpi || 200;

        PDFJS.getDocument(filePath).then(function (pdf) {
            var info = {
                id: pdf.fingerprint,
                pages: pdf.numPages
            };
            window.external.PdfOpened(info.id, info.pages);
            renderPage(pdf, 1, info.pages);

        }, function (error) {
            alert('failed doc');
            window.external.Failed(error);
        });
    }

    function renderPage(pdf, pageNum, totalPages) {
        pdf.getPage(pageNum).then(function (page) {

            var scale = _dpi / 72; // scale from 72 
            var scaledViewport = page.getViewport(scale);
            //alert(scaledViewport.width + ', ' + scaledViewport.height);

            var canvas = document.getElementById('the-canvas');
            var context = canvas.getContext('2d');
            canvas.height = scaledViewport.height;
            canvas.width = scaledViewport.width;

            var renderContext = {
                canvasContext: context,
                viewport: scaledViewport
            };
            page.render(renderContext).promise.then(function () {
                var pngData = canvas.toDataURL("image/png");
                window.external.PageRendered(pageNum, pngData);

                if (pageNum < totalPages) {
                    renderPage(pdf, pageNum + 1, totalPages);
                } else {
                    window.external.RenderCompleted();
                }

            }, function (error) {
                window.external.Failed(error);
            });


        }, function (error) {
            window.external.Failed(error);
        });
    }

    return self;
})();

function renderPdf(filePath, dpi) {
    if (window.external && filePath && PDFJS) {
        renderer.start(filePath, dpi);
    }
}

