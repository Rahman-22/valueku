// Mobile sidebar toggle
(function () {
    const toggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    if (toggle && sidebar) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });
        // Close the sidebar when a link is tapped on small screens.
        sidebar.querySelectorAll('a').forEach(function (link) {
            link.addEventListener('click', function () {
                if (window.innerWidth < 992) sidebar.classList.remove('open');
            });
        });
    }
})();

// Show/hide toggle for password fields
(function () {
    function enhance(input) {
        if (input.dataset.eye) return;
        input.dataset.eye = '1';

        var group = document.createElement('div');
        group.className = 'input-group';
        input.parentNode.insertBefore(group, input);
        group.appendChild(input);

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.tabIndex = -1;
        btn.className = 'btn btn-outline-secondary';
        btn.setAttribute('aria-label', 'Show or hide password');
        btn.innerHTML = '<i class="bi bi-eye"></i>';
        btn.addEventListener('click', function () {
            var hidden = input.type === 'password';
            input.type = hidden ? 'text' : 'password';
            btn.innerHTML = hidden ? '<i class="bi bi-eye-slash"></i>' : '<i class="bi bi-eye"></i>';
        });
        group.appendChild(btn);
    }

    document.querySelectorAll('input[type="password"]').forEach(enhance);
})();
