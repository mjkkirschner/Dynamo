using Dynamo.Models;
using Dynamo.PackageManager;

namespace Dynamo.ViewModels
{
    public delegate void ImageSaveEventHandler(object sender, ImageSaveEventArgs e);

    public delegate void WorkspaceSaveEventHandler(object sender, WorkspaceSaveEventArgs e);

    public delegate void RequestPackagePublishDialogHandler(PublishPackageViewModel publishViewModel);

    public delegate void RequestAboutWindowHandler(DynamoViewModel aboutViewModel);

    internal delegate void RequestViewOperationHandler(ViewOperationEventArgs e);

    public delegate void RequestBitmapSourceHandler(IconRequestEventArgs e);

    public delegate void RequestOpenDocumentationLinkHandler(OpenDocumentationLinkEventArgs e);
}
