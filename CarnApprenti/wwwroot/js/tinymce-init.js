window.tinyMCEInit = (elementId) => {
    tinymce.init({
        selector: `#${elementId}`,
        menubar: false,
        statusbar: false,
        toolbar: 'undo redo | bold italic underline | alignleft aligncenter alignright | bullist numlist | removeformat',
        plugins: 'lists advlist',
        height: 300
    });
};

window.tinyMCEGetContent = (elementId) => {
    return tinymce.get(elementId).getContent();
};

window.tinyMCESetContent = (elementId, content) => {
    tinymce.get(elementId).setContent(content);
};
