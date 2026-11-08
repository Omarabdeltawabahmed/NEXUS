// ==========================================================================
// NEXUS Core - High Performance Interactive Scripts
// Transitions, Instant Search, & Notification Badge Synchronization
// ==========================================================================

document.addEventListener('DOMContentLoaded', function () {
    initFastTransitions();
    initGlobalInstantSearch();
    initNotificationsManagement();
    initMobileSidebarDrawer();
});

// --------------------------------------------------------------------------
// 1. Ultra-Fast & Smooth Page Transitions
// --------------------------------------------------------------------------
function initFastTransitions() {
    const contentWrapper = document.getElementById('pageContentWrapper');

    // Create or get top progress bar
    let progressBar = document.getElementById('nexusNavProgressBar');
    if (!progressBar) {
        progressBar = document.createElement('div');
        progressBar.id = 'nexusNavProgressBar';
        document.body.appendChild(progressBar);
    }

    // Reset state on page show
    window.addEventListener('pageshow', function () {
        if (contentWrapper) {
            contentWrapper.classList.remove('page-transition-exit');
        }
        if (progressBar) {
            progressBar.style.width = '100%';
            setTimeout(function () {
                progressBar.style.opacity = '0';
                setTimeout(function () {
                    progressBar.style.width = '0%';
                    progressBar.style.opacity = '1';
                }, 200);
            }, 80);
        }
    });

    // Hover speculative prefetching for internal links to make loading instant
    const prefetchedUrls = new Set();
    function prefetchUrl(urlStr) {
        if (!urlStr || prefetchedUrls.has(urlStr)) return;
        try {
            const parsed = new URL(urlStr, window.location.origin);
            if (parsed.origin === window.location.origin && parsed.pathname !== window.location.pathname) {
                prefetchedUrls.add(urlStr);
                const linkElem = document.createElement('link');
                linkElem.rel = 'prefetch';
                linkElem.href = urlStr;
                document.head.appendChild(linkElem);
            }
        } catch (e) {}
    }

    document.addEventListener('mouseover', function (e) {
        const link = e.target.closest('a');
        if (link && link.href) {
            prefetchUrl(link.href);
        }
    });

    // Immediate non-blocking link clicks
    document.addEventListener('click', function (e) {
        const link = e.target.closest('a');
        if (!link) return;

        const href = link.getAttribute('href');
        if (!href) return;

        // Skip non-navigational links
        if (
            href.startsWith('#') ||
            href.startsWith('javascript:') ||
            link.getAttribute('target') === '_blank' ||
            link.hasAttribute('download') ||
            e.ctrlKey || e.metaKey || e.shiftKey || e.altKey ||
            e.button !== 0
        ) {
            return;
        }

        try {
            const url = new URL(link.href, window.location.origin);
            if (url.origin === window.location.origin && url.pathname !== window.location.pathname) {
                // Immediate visual feedback without delaying browser navigation
                if (progressBar) {
                    progressBar.style.opacity = '1';
                    progressBar.style.width = '70%';
                }
                if (contentWrapper) {
                    contentWrapper.classList.add('page-transition-exit');
                }
            }
        } catch (err) {}
    });
}

// --------------------------------------------------------------------------
// 2. Interactive Global Search (Click to Instant Browse + Type to Filter)
// --------------------------------------------------------------------------
function initGlobalInstantSearch() {
    const searchInput = document.getElementById('nexusGlobalSearch');
    const resultsContainer = document.getElementById('nexusSearchResults');
    const searchIcon = document.getElementById('nexusSearchIcon');
    const searchSpinner = document.getElementById('nexusSearchSpinner');
    const clearBtn = document.getElementById('nexusSearchClearBtn');
    const kbdShortcut = document.getElementById('nexusSearchKbdShortcut');

    if (!searchInput || !resultsContainer) return;

    let debounceTimer = null;
    let currentSelectedIndex = -1;
    let cachedQuickBrowse = null;

    // Pre-load quick browse data in background so clicking is 0ms instant
    fetchQuickBrowseData();

    async function fetchQuickBrowseData() {
        try {
            const res = await fetch('/Home/GlobalSearch');
            if (res.ok) {
                cachedQuickBrowse = await res.json();
            }
        } catch (e) {}
    }

    // Global keyboard shortcut: Ctrl+K or Cmd+K
    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            searchInput.focus();
            searchInput.select();
            showSearchMenu();
        }
    });

    // When clicking or focusing on search input: IMMEDIATELY show categories
    searchInput.addEventListener('click', function () {
        showSearchMenu();
    });

    searchInput.addEventListener('focus', function () {
        showSearchMenu();
    });

    function showSearchMenu() {
        const query = searchInput.value.trim();
        if (query.length > 0) {
            executeSearch(query);
        } else {
            // Show all categories immediately from cache or fetch
            if (cachedQuickBrowse) {
                renderSearchResults('', cachedQuickBrowse, true);
            } else {
                executeSearch('');
            }
        }
    }

    // Clear button click
    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            searchInput.value = '';
            clearBtn.classList.add('hidden');
            if (kbdShortcut) kbdShortcut.classList.remove('hidden');
            searchInput.focus();
            if (cachedQuickBrowse) {
                renderSearchResults('', cachedQuickBrowse, true);
            } else {
                executeSearch('');
            }
        });
    }

    // Live typing: filter results as soon as 1st character is typed
    searchInput.addEventListener('input', function () {
        const query = searchInput.value.trim();

        if (query.length > 0) {
            if (clearBtn) clearBtn.classList.remove('hidden');
            if (kbdShortcut) kbdShortcut.classList.add('hidden');
        } else {
            if (clearBtn) clearBtn.classList.add('hidden');
            if (kbdShortcut) kbdShortcut.classList.remove('hidden');
            if (cachedQuickBrowse) {
                renderSearchResults('', cachedQuickBrowse, true);
                return;
            }
        }

        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(function () {
            executeSearch(query);
        }, 150);
    });

    // Keyboard navigation
    searchInput.addEventListener('keydown', function (e) {
        const items = resultsContainer.querySelectorAll('.search-result-item');
        if (resultsContainer.classList.contains('hidden') || items.length === 0) {
            if (e.key === 'Escape') {
                searchInput.blur();
            }
            return;
        }

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            currentSelectedIndex = (currentSelectedIndex + 1) % items.length;
            updateActiveItem(items);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            currentSelectedIndex = (currentSelectedIndex - 1 + items.length) % items.length;
            updateActiveItem(items);
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (currentSelectedIndex >= 0 && currentSelectedIndex < items.length) {
                items[currentSelectedIndex].click();
            } else if (items.length > 0) {
                items[0].click();
            }
        } else if (e.key === 'Escape') {
            e.preventDefault();
            resultsContainer.classList.add('hidden');
            searchInput.blur();
        }
    });

    function updateActiveItem(items) {
        items.forEach(function (item, idx) {
            if (idx === currentSelectedIndex) {
                item.classList.add('active-item', 'bg-slate-800/80');
                item.scrollIntoView({ block: 'nearest' });
            } else {
                item.classList.remove('active-item', 'bg-slate-800/80');
            }
        });
    }

    // Close when clicking outside
    document.addEventListener('click', function (e) {
        const searchWrapper = document.getElementById('nexusSearchWrapper');
        if (searchWrapper && !searchWrapper.contains(e.target)) {
            resultsContainer.classList.add('hidden');
        }
    });

    // Execute search request
    async function executeSearch(query) {
        if (query.length > 0) {
            if (searchIcon) searchIcon.classList.add('hidden');
            if (searchSpinner) searchSpinner.classList.remove('hidden');
        }

        try {
            const url = query.length > 0
                ? `/Home/GlobalSearch?query=${encodeURIComponent(query)}`
                : '/Home/GlobalSearch';

            const response = await fetch(url);
            if (!response.ok) throw new Error('Search failed');

            const data = await response.json();
            if (query.length === 0) {
                cachedQuickBrowse = data;
            }
            renderSearchResults(query, data, data.isQuickBrowse);
        } catch (error) {
            console.error('Search error:', error);
        } finally {
            if (searchSpinner) searchSpinner.classList.add('hidden');
            if (searchIcon) searchIcon.classList.remove('hidden');
        }
    }

    // Render results HTML
    function renderSearchResults(query, data, isQuickBrowse) {
        const departments = data.departments || [];
        const projects = data.projects || [];
        const tasks = data.tasks || [];
        const teams = data.teams || [];

        const totalResults = departments.length + projects.length + tasks.length + teams.length;
        currentSelectedIndex = -1;

        if (totalResults === 0) {
            resultsContainer.innerHTML = `
                <div class="p-8 text-center text-slate-400">
                    <div class="w-12 h-12 mx-auto rounded-xl bg-slate-800/60 border border-slate-700/60 flex items-center justify-center text-slate-500 mb-3">
                        <i class="fa-solid fa-magnifying-glass text-lg"></i>
                    </div>
                    <p class="text-sm font-semibold text-slate-200">No results found for "${escapeHtml(query)}"</p>
                    <p class="text-xs text-slate-400 mt-1">Try another keyword or search for department, project, task, or team.</p>
                </div>
            `;
            resultsContainer.classList.remove('hidden');
            resultsContainer.classList.add('dropdown-animate-enter');
            return;
        }

        let html = '';

        // Header indicator
        if (isQuickBrowse) {
            html += `
                <div class="px-4 py-2 bg-slate-950/60 border-b border-slate-800/80 flex items-center justify-between text-xs text-slate-400">
                    <span class="flex items-center gap-1.5 font-medium text-slate-300">
                        <i class="fa-solid fa-compass text-cyan-400"></i> Explore NEXUS
                    </span>
                    <span class="text-[11px] text-slate-500">Click to open, or type to search</span>
                </div>
            `;
        } else {
            html += `
                <div class="px-4 py-2 bg-slate-950/60 border-b border-slate-800/80 flex items-center justify-between text-xs text-slate-400">
                    <span class="flex items-center gap-1.5 font-medium text-slate-300">
                        <i class="fa-solid fa-magnifying-glass text-cyan-400"></i> Results for "${escapeHtml(query)}"
                    </span>
                    <span class="text-[11px] font-semibold text-cyan-400">${totalResults} found</span>
                </div>
            `;
        }

        function renderCategory(title, items, icon, colorClass, borderClass, bgClass) {
            if (items.length === 0) return '';

            let catHtml = `
                <div class="p-2">
                    <div class="px-3 py-1.5 text-[11px] font-bold uppercase tracking-wider text-slate-400 flex items-center gap-2">
                        <i class="${icon} ${colorClass}"></i>
                        <span>${title}</span>
                        <span class="ml-auto text-[10px] text-slate-500 font-semibold">${items.length}</span>
                    </div>
                    <div class="space-y-1 mt-1">
            `;

            items.forEach(function (item) {
                catHtml += `
                    <a href="${item.url}" class="search-result-item flex items-center justify-between px-3 py-2 rounded-xl hover:bg-slate-800/70 border border-transparent hover:border-slate-700/60 transition-all duration-150 group">
                        <div class="flex items-center gap-3 min-w-0 flex-1">
                            <div class="w-8 h-8 rounded-lg ${bgClass} ${colorClass} border ${borderClass} flex items-center justify-center text-xs flex-shrink-0">
                                <i class="${item.icon}"></i>
                            </div>
                            <div class="min-w-0 flex-1">
                                <h4 class="text-sm font-semibold text-slate-200 group-hover:text-cyan-400 transition-colors truncate">
                                    ${escapeHtml(item.title)}
                                </h4>
                                <p class="text-[11px] text-slate-400 truncate">
                                    ${escapeHtml(item.subtitle)}
                                </p>
                            </div>
                        </div>
                        <div class="flex items-center gap-2 ml-3 flex-shrink-0">
                            <span class="text-[10px] px-2 py-0.5 rounded-md font-medium border ${borderClass} ${colorClass} ${bgClass}">
                                ${escapeHtml(item.badge)}
                            </span>
                            <i class="fa-solid fa-arrow-right text-[10px] text-slate-500 group-hover:text-cyan-400 group-hover:translate-x-1 transition-all"></i>
                        </div>
                    </a>
                `;
            });

            catHtml += `
                    </div>
                </div>
            `;
            return catHtml;
        }

        html += renderCategory('Departments', departments, 'fa-solid fa-building', 'text-cyan-400', 'border-cyan-500/30', 'bg-cyan-500/10');
        html += renderCategory('Projects', projects, 'fa-solid fa-layer-group', 'text-emerald-400', 'border-emerald-500/30', 'bg-emerald-500/10');
        html += renderCategory('Tasks', tasks, 'fa-solid fa-square-check', 'text-violet-400', 'border-violet-500/30', 'bg-violet-500/10');
        html += renderCategory('Teams', teams, 'fa-solid fa-users', 'text-amber-400', 'border-amber-500/30', 'bg-amber-500/10');

        html += `
            <div class="p-2.5 bg-slate-950/70 border-t border-slate-800 text-[11px] text-slate-400 flex items-center justify-between px-4">
                <span class="flex items-center gap-1.5">
                    <kbd class="px-1.5 py-0.5 rounded bg-slate-800 border border-slate-700 text-slate-300 font-mono text-[10px]">↑</kbd>
                    <kbd class="px-1.5 py-0.5 rounded bg-slate-800 border border-slate-700 text-slate-300 font-mono text-[10px]">↓</kbd>
                    <span>navigate</span>
                </span>
                <span class="flex items-center gap-1.5">
                    <kbd class="px-1.5 py-0.5 rounded bg-slate-800 border border-slate-700 text-slate-300 font-mono text-[10px]">Enter</kbd>
                    <span>open</span>
                </span>
                <span class="flex items-center gap-1.5">
                    <kbd class="px-1.5 py-0.5 rounded bg-slate-800 border border-slate-700 text-slate-300 font-mono text-[10px]">Esc</kbd>
                    <span>close</span>
                </span>
            </div>
        `;

        resultsContainer.innerHTML = html;
        resultsContainer.classList.remove('hidden');
        resultsContainer.classList.add('dropdown-animate-enter');
    }
}

// --------------------------------------------------------------------------
// 3. Notifications Dropdown & Seen Tasks Synchronization
// --------------------------------------------------------------------------
function initNotificationsManagement() {
    const bellBtn = document.getElementById('notificationsBellBtn');
    const dropdown = document.getElementById('notificationsDropdown');
    const wrapper = document.getElementById('notificationsDropdownWrapper');
    const badge = document.getElementById('notificationsBadge');
    const headerBadge = document.getElementById('notificationsPendingHeaderBadge');
    const currentUserId = bellBtn ? (bellBtn.getAttribute('data-user-id') || 'guest') : 'guest';

    const storageKey = 'nexus_seen_tasks_' + currentUserId;

    // Retrieve set of seen task IDs from localStorage
    function getSeenTasks() {
        try {
            const data = localStorage.getItem(storageKey);
            return data ? JSON.parse(data) : [];
        } catch (e) {
            return [];
        }
    }

    function markTaskAsSeen(taskId) {
        if (!taskId) return;
        const seen = getSeenTasks();
        if (!seen.includes(String(taskId))) {
            seen.push(String(taskId));
            try {
                localStorage.setItem(storageKey, JSON.stringify(seen));
            } catch (e) {}
        }
    }

    // Synchronize UI badge and task items with seen tasks
    function syncSeenState() {
        const seen = getSeenTasks();
        const taskLinks = document.querySelectorAll('.notification-task-link');
        let unreadCount = 0;

        taskLinks.forEach(function (link) {
            const taskId = link.getAttribute('data-task-id');
            const isSeen = taskId && seen.includes(String(taskId));

            if (isSeen) {
                // Task has been seen by the user: update badge to Viewed
                const badgeEl = link.querySelector('.task-status-badge');
                if (badgeEl) {
                    badgeEl.textContent = 'Viewed';
                    badgeEl.className = 'task-status-badge px-1.5 py-0.5 rounded text-[9px] font-medium bg-slate-800 text-slate-400 border border-slate-700/60 flex-shrink-0';
                }
            } else {
                unreadCount++;
            }
        });

        // Update or hide the bell badge
        if (badge) {
            if (unreadCount <= 0) {
                badge.classList.add('hidden');
                badge.style.display = 'none';
            } else {
                badge.classList.remove('hidden');
                badge.style.display = 'flex';
                badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
            }
        }

        // Update header badge
        if (headerBadge) {
            if (unreadCount <= 0) {
                headerBadge.classList.add('hidden');
                headerBadge.style.display = 'none';
            } else {
                headerBadge.classList.remove('hidden');
                headerBadge.style.display = 'inline-block';
                headerBadge.textContent = unreadCount + ' Required';
            }
        }
    }

    syncSeenState();

    // When clicking any task in the notifications list:
    // Instantly mark it seen, hide/decrement the badge, and navigate to the task
    document.addEventListener('click', function (e) {
        const taskLink = e.target.closest('.notification-task-link');
        if (!taskLink) return;

        const taskId = taskLink.getAttribute('data-task-id');
        if (taskId) {
            markTaskAsSeen(taskId);
        }

        // Immediate visual feedback: hide or decrease badge right now
        if (badge) {
            let current = parseInt(badge.textContent.trim()) || 1;
            current = Math.max(0, current - 1);
            if (current <= 0) {
                badge.classList.add('hidden');
                badge.style.display = 'none';
            } else {
                badge.textContent = current;
            }
        }

        syncSeenState();
    });

    if (!bellBtn || !dropdown || !wrapper) return;

    // Toggle notifications dropdown
    bellBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        const isHidden = dropdown.classList.contains('hidden');

        if (isHidden) {
            dropdown.classList.remove('hidden');
            dropdown.classList.add('dropdown-animate-enter');
            bellBtn.setAttribute('aria-expanded', 'true');
        } else {
            dropdown.classList.add('hidden');
            bellBtn.setAttribute('aria-expanded', 'false');
        }
    });

    // Close on click outside
    document.addEventListener('click', function (e) {
        if (!wrapper.contains(e.target)) {
            dropdown.classList.add('hidden');
            bellBtn.setAttribute('aria-expanded', 'false');
        }
    });

    // Close on Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !dropdown.classList.contains('hidden')) {
            dropdown.classList.add('hidden');
            bellBtn.setAttribute('aria-expanded', 'false');
            bellBtn.focus();
        }
    });
}

// Helper: Escape HTML
function escapeHtml(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

// --------------------------------------------------------------------------
// 4. Responsive Mobile Sidebar Navigation Drawer
// --------------------------------------------------------------------------
function initMobileSidebarDrawer() {
    const toggleBtn = document.getElementById('mobileSidebarToggle');
    const closeBtn = document.getElementById('mobileSidebarClose');
    const backdrop = document.getElementById('mobileSidebarBackdrop');
    const sidebar = document.getElementById('nexusSidebar');

    if (!sidebar) return;

    function openDrawer() {
        sidebar.classList.remove('-translate-x-full');
        if (backdrop) backdrop.classList.remove('hidden');
        document.body.classList.add('overflow-hidden', 'md:overflow-auto');
    }

    function closeDrawer() {
        sidebar.classList.add('-translate-x-full');
        if (backdrop) backdrop.classList.add('hidden');
        document.body.classList.remove('overflow-hidden', 'md:overflow-auto');
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            openDrawer();
        });
    }

    if (closeBtn) {
        closeBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            closeDrawer();
        });
    }

    if (backdrop) {
        backdrop.addEventListener('click', function () {
            closeDrawer();
        });
    }

    // Close on Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !sidebar.classList.contains('-translate-x-full') && window.innerWidth < 768) {
            closeDrawer();
        }
    });

    // Close drawer when clicking any link inside sidebar on mobile
    const navLinks = sidebar.querySelectorAll('a');
    navLinks.forEach(function (link) {
        link.addEventListener('click', function () {
            if (window.innerWidth < 768) {
                closeDrawer();
            }
        });
    });

    // Auto-close drawer if viewport resized to desktop
    window.addEventListener('resize', function () {
        if (window.innerWidth >= 768) {
            if (backdrop) backdrop.classList.add('hidden');
            document.body.classList.remove('overflow-hidden', 'md:overflow-auto');
        }
    });
}

