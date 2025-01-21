namespace Swole
{
    public static class EditPackageState
    {
        private static PackageManifest targetPackage;
        public static PackageManifest TargetPackage
        {
            get => targetPackage;
            set => SetTargetPackage(value);
        }
        public static void SetTargetPackage(PackageManifest manifest)
        {
            targetPackage = manifest;
        }

        private static string currentAuthor;
        public static string CurrentAuthor
        {
            get => string.IsNullOrWhiteSpace(currentAuthor) ? targetPackage.Curator : currentAuthor;
            set => SetCurrentAuthor(value);
        }
        public static void SetCurrentAuthor(string author)
        {
            currentAuthor = author;
        }

        private static IContent targetContent;
        public static IContent TargetContent
        {
            get => targetContent;
            set => targetContent = value; 
        }

        private static string swapBackScene;
        public static string SwapBackScene
        {
            get => swapBackScene;
            set => swapBackScene = value;
        }
        public static string ConsumeSwapBackScene()
        {
            string scene = SwapBackScene;
            swapBackScene = null;
            return scene;
        }
    }
}
