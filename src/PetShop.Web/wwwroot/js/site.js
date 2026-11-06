const search = document.querySelector('#memberSearch');
const searchableRows = [...document.querySelectorAll('[data-search]')];
search?.addEventListener('input', () => {
    const query = search.value.trim().toLocaleLowerCase('zh-CN');
    searchableRows.forEach((row) => { row.hidden = query.length > 0 && !row.dataset.search.toLocaleLowerCase('zh-CN').includes(query); });
});
const toast = document.querySelector('#demoToast');
document.querySelectorAll('[data-demo-message]').forEach((button) => {
    button.addEventListener('click', () => {
        if (!toast) return;
        toast.textContent = button.dataset.demoMessage; toast.classList.add('show');
        window.clearTimeout(window.petShopToastTimer); window.petShopToastTimer = window.setTimeout(() => toast.classList.remove('show'), 3200);
    });
});

const path = window.location.pathname.toLowerCase();
const navigationLinks = [...document.querySelectorAll('.nav-link')];
const activeLink = navigationLinks
    .filter((link) => {
        const href = (link.getAttribute('href') || '').toLowerCase();
        return href === '/' ? path === '/' : path === href || path.startsWith(`${href}/`);
    })
    .sort((left, right) => (right.getAttribute('href') || '').length - (left.getAttribute('href') || '').length)[0];

navigationLinks.forEach((link) => {
    link.classList.toggle('active', link === activeLink);
});

const mobileLinks = [...document.querySelectorAll('.mobile-nav-link')];
const mobileSection = path.startsWith('/members') ? '/members' : path.startsWith('/grooming') ? '/grooming' : path.startsWith('/products') ? '/products' : path === '/' || path.startsWith('/scan') || path.startsWith('/meituan/redeem') ? '/' : '/manage';
mobileLinks.forEach((link) => {
    const href = (link.getAttribute('href') || '').toLowerCase();
    const section = href === '/' ? '/' : href.split('/').slice(0, 2).join('/');
    link.classList.toggle('active', section === mobileSection);
});

if ('serviceWorker' in navigator && window.isSecureContext) {
    window.addEventListener('load', () => navigator.serviceWorker.register('/service-worker.js').catch(() => {}));
}
