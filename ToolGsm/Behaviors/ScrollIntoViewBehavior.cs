namespace ToolGsm.Behaviors;

public class ScrollIntoViewBehavior : Behavior<ListView>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += AssociatedObject_Loaded;
        AssociatedObject.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= AssociatedObject_Loaded;
        AssociatedObject.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
        base.OnDetaching();
    }

    private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        ScrollToBottom();
    }

    private void ItemContainerGenerator_StatusChanged(object sender, System.EventArgs e)
    {
        if (AssociatedObject.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        {
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (AssociatedObject.Items.Count > 0)
        {
            var lastItem = AssociatedObject.Items[AssociatedObject.Items.Count - 1];
            AssociatedObject.ScrollIntoView(lastItem);
        }
    }
}