var MyPlugin = {
  Hello: function () {
    window.alert("Hello, World");
  },
  HelloString: function (str) {
    window.alert(Pointer_stringify(str));
  },
};
mergeInto(LibraryManager.library, MyPlugin);