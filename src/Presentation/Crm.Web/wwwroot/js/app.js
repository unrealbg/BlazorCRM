(function(){
  const qs = s => document.querySelector(s);
  const qsa = s => Array.from(document.querySelectorAll(s));

  function setTheme(isDark, persist){
    if(persist){
      localStorage.setItem('theme', isDark ? 'dark' : 'light');
    }
    const root = document.documentElement;
    root.classList.toggle('dark', isDark);
    root.dataset.theme = isDark ? 'dark' : 'light';
    root.style.colorScheme = isDark ? 'dark' : 'light';
    if(document.body){
      document.body.classList.toggle('dark', isDark);
      document.body.dataset.theme = isDark ? 'dark' : 'light';
    }
  }

  function getThemePreference(){
    const saved = localStorage.getItem('theme');
    if(saved === 'dark' || saved === 'light') return saved;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function applyTheme(){
    const pref = getThemePreference();
    setTheme(pref === 'dark', false);
  }

  function toggleTheme(){
    const isDark = document.documentElement.classList.contains('dark');
    setTheme(!isDark, true);
  }

  function openModal(id){ const el = qs('#'+id); if(el) el.classList.remove('hidden'); }
  function closeModal(id){ const el = qs('#'+id); if(el) el.classList.add('hidden'); }

  let paletteEl = null;
  let paletteInput = null;
  let palettePanel = null;
  function openPalette(){
    paletteEl = paletteEl || qs('#commandPalette');
    paletteInput = paletteInput || qs('#paletteSearch');
    palettePanel = palettePanel || qs('#paletteResults');
    if(!paletteEl || !paletteInput || !palettePanel) return;
    paletteEl.classList.remove('hidden');
    paletteInput.focus();
    paletteInput.select();
  }
  function closePalette(){
    if(!paletteEl || !palettePanel) return;
    paletteEl.classList.add('hidden');
    palettePanel.classList.add('hidden');
    palettePanel.innerHTML = '';
  }
  function showToast(text, kind){
    const wrap = qs('#toasts'); if(!wrap) return;
    const div = document.createElement('div');
    div.className = 'card px-3 py-2 text-sm';
    div.textContent = text;
    wrap.appendChild(div);
    setTimeout(()=> div.remove(), 2500);
  }

  const subs = new Map();
  function publish(name, detail){
    window.dispatchEvent(new CustomEvent(name, { detail }));
  }
  function subscribe(name, dotNetRef, method){
    const key = name + ':' + Math.random().toString(36).slice(2);
    const handler = e => {
      try { dotNetRef.invokeMethodAsync(method, e.detail ?? null); } catch { /* no-op */ }
    };
    window.addEventListener(name, handler);
    subs.set(key, { name, handler });
    return key;
  }
  function unsubscribe(key){
    const s = subs.get(key);
    if(!s) return;
    window.removeEventListener(s.name, s.handler);
    subs.delete(key);
  }

  function ensureSidebarBackdrop(){
    let ov = qs('#sidebar-backdrop');
    if(!ov){
      ov = document.createElement('div');
      ov.id = 'sidebar-backdrop';
      ov.className = 'fixed inset-0 bg-black/40 z-40';
      ov.addEventListener('click', closeSidebar);
      document.body.appendChild(ov);
    }
    return ov;
  }

  function isDesktop(){
    return window.matchMedia('(min-width: 1024px)').matches; // lg breakpoint
  }

  function openSidebar(){
    const sb = qs('#sidebar');
    if(!sb) return;
    sb.classList.remove('hidden');
    sb.classList.add('flex');
    if(!isDesktop()){
      const ov = ensureSidebarBackdrop();
      ov.style.display = 'block';
      document.body.style.overflow = 'hidden';
    }
  }
  function closeSidebar(){
    const sb = qs('#sidebar');
    if(!sb) return;
    sb.classList.add('hidden');
    sb.classList.remove('flex');
    const ov = qs('#sidebar-backdrop');
    if(ov) ov.style.display = 'none';
    document.body.style.overflow = '';
  }
  function toggleSidebar(){
    const sb = qs('#sidebar');
    if(!sb) return;
    const isHidden = sb.classList.contains('hidden');
    if(isHidden) openSidebar(); else closeSidebar();
  }

  function wireSidebarAutoClose(){
    const container = qs('#sidebar');
    if(!container) return;
    container.addEventListener('click', (e) => {
      const a = e.target.closest('a');
      if(!a) return;
      if(!isDesktop()) {
        setTimeout(closeSidebar, 0);
      }
    });
  }

  function wireUserMenu(){
    const btn = qs('#userMenuBtn');
    const menu = qs('#userMenu');
    if(!btn || !menu) return;

    btn.addEventListener('click', (e)=>{
      e.stopPropagation();
      menu.classList.toggle('hidden');
    });

    document.addEventListener('click', (e)=>{
      if(menu.classList.contains('hidden')) return;
      if(e.target.closest('#userMenu') || e.target.closest('#userMenuBtn')) return;
      menu.classList.add('hidden');
    });
  }

  function wireNotifications(){
    const btn = qs('#notifBtn');
    if(!btn) return;
    btn.addEventListener('click', ()=>{
      showToast('Notifications panel is not wired yet', 'info');
    });
  }

  function wirePaletteShortcut(){
    document.addEventListener('keydown', (e)=>{
      const isMac = navigator.platform.toUpperCase().includes('MAC');
      const combo = isMac
        ? e.metaKey && e.shiftKey && e.key.toLowerCase() === 'k'
        : e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 'k';
      if(combo){
        e.preventDefault();
        e.stopPropagation();
        openPalette();
      }
    }, true);
  }

  function wireSearch(input, panel, options){
    if(!input || !panel) return () => {};

    let items = [];
    let selected = -1;
    let timer = null;

    const close = () => {
      panel.classList.add('hidden');
      panel.innerHTML = '';
      items = [];
      selected = -1;
      options?.onClose?.();
    };

    const render = () => {
      panel.innerHTML = '';
      if(items.length === 0){
        panel.innerHTML = '<div class="px-3 py-2 text-sm text-slate-500">No results</div>';
      } else {
        items.forEach((item, idx) => {
          const row = document.createElement('div');
          row.className = 'px-3 py-2 rounded-lg cursor-pointer flex flex-col gap-1 ' + (idx === selected ? 'bg-white/60 dark:bg-slate-700/50' : '');
          row.dataset.url = item.url;
          row.innerHTML = `<div class="text-sm font-medium">${item.title}</div><div class="text-xs text-slate-500">${item.subtitle ?? item.type}</div>`;
          row.addEventListener('mouseenter', () => { selected = idx; render(); });
          row.addEventListener('mousedown', (e) => {
            e.preventDefault();
            window.location.href = item.url;
            if(options?.closeOnSelect) close();
          });
          panel.appendChild(row);
        });
      }
      panel.classList.remove('hidden');
    };

    const fetchResults = async (q) => {
      if(q.length < 2){
        close();
        return;
      }
      try {
        const res = await fetch(`/api/search?q=${encodeURIComponent(q)}`, { credentials: 'include' });
        if(!res.ok){
          close();
          return;
        }
        items = await res.json();
        selected = items.length > 0 ? 0 : -1;
        render();
      } catch {
        close();
      }
    };

    input.addEventListener('input', () => {
      const q = input.value.trim();
      if(timer) clearTimeout(timer);
      timer = setTimeout(() => fetchResults(q), 200);
    });

    input.addEventListener('keydown', (e) => {
      if(panel.classList.contains('hidden')) return;
      if(e.key === 'ArrowDown'){
        e.preventDefault();
        selected = Math.min(items.length - 1, selected + 1);
        render();
      } else if(e.key === 'ArrowUp'){
        e.preventDefault();
        selected = Math.max(0, selected - 1);
        render();
      } else if(e.key === 'Enter'){
        if(selected >= 0 && items[selected]){
          window.location.href = items[selected].url;
          if(options?.closeOnSelect) close();
        }
      } else if(e.key === 'Escape'){
        if(options?.onEscape){
          options.onEscape();
        } else {
          close();
        }
      }
    });

    return close;
  }

  function wireGlobalSearch(){
    const input = qs('#globalSearch');
    const panel = qs('#globalSearchResults');
    if(!input || !panel) return;
    const close = wireSearch(input, panel, { closeOnSelect: true });
    document.addEventListener('click', (e) => {
      if(e.target.closest('#globalSearch') || e.target.closest('#globalSearchResults')) return;
      close();
    });
  }

  function wirePalette(){
    paletteEl = qs('#commandPalette');
    paletteInput = qs('#paletteSearch');
    palettePanel = qs('#paletteResults');
    if(!paletteEl || !paletteInput || !palettePanel) return;

    const close = () => { closePalette(); };
    const closeSearch = wireSearch(paletteInput, palettePanel, { closeOnSelect: true, onEscape: close });

    paletteEl.addEventListener('click', (e) => {
      if(e.target.closest('[data-palette-close]')){
        closeSearch();
        close();
      }
    });
  }

  window.applyTheme = applyTheme;
  window.toggleTheme = toggleTheme;
  window.setTheme = setTheme;
  window.getThemePreference = getThemePreference;
  window.openModal = openModal;
  window.closeModal = closeModal;
  window.openPalette = openPalette;
  window.showToast = showToast;
  window.toggleSidebar = toggleSidebar;
  window.openSidebar = openSidebar;
  window.closeSidebar = closeSidebar;
  window.publish = publish;
  window.subscribe = subscribe;
  window.unsubscribe = unsubscribe;

  applyTheme();
  document.addEventListener('DOMContentLoaded', ()=>{
    wireSidebarAutoClose();
    wireUserMenu();
    wireNotifications();
    wirePaletteShortcut();
    wireGlobalSearch();
    wirePalette();
    const year = document.getElementById('year');
    if(year) year.textContent = new Date().getFullYear();

    window.addEventListener('resize', ()=>{
      if(isDesktop()){
        const ov = qs('#sidebar-backdrop');
        if(ov) ov.style.display = 'none';
        document.body.style.overflow = '';
      }
    });
  });

  const media = window.matchMedia('(prefers-color-scheme: dark)');
  media.addEventListener('change', (e)=>{
    const saved = localStorage.getItem('theme');
    if(!saved){
      setTheme(e.matches, false);
    }
  });
})();
