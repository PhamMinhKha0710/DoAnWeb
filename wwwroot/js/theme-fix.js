/**
 * Theme Fix Utility
 * This script provides utility functions to fix theme-related styling issues
 */
(function() {
    // Kiểm tra xem phần tử đã có style inline hay chưa
    function hasInlineStyle(element, property) {
        return element.style[property] !== '';
    }
    
    // Kiểm tra xem phần tử có thuộc tính !important trong inline style hay không
    function hasImportantStyle(element, computedStyle, property) {
        return element.style[property] !== '' && 
               computedStyle.getPropertyPriority(property) === 'important';
    }

    // Utility to fix tag page styling
    function fixTagPageStyling() {
        const isDarkTheme = document.documentElement.getAttribute('data-theme') === 'dark';
        console.log('Applying tag page styling fixes for theme:', isDarkTheme ? 'dark' : 'light');
        
        // Đảm bảo tags-grid-container sử dụng grid layout
        refreshGridLayout();
        
        // Fix tag cards
        document.querySelectorAll('.tag-card').forEach(card => {
            // Check if card is properly displayed
            const computedStyle = getComputedStyle(card);
            const isVisible = computedStyle.display !== 'none' && 
                             computedStyle.visibility !== 'hidden' &&
                             card.offsetHeight > 10;
            
            if (!isVisible) {
                console.log('Fixing invisible card...');
                // Reset any problematic styles that might be causing invisibility
                card.style.visibility = 'visible';
                card.style.opacity = '1';
                card.style.height = '';
                
                // Apply basic visibility styling but respect existing inline styles
                if (!hasInlineStyle(card, 'display')) {
                    card.style.display = 'flex';
                }
                
                if (!hasInlineStyle(card, 'flexDirection')) {
                    card.style.flexDirection = 'column';
                }
            }
            
            // Set or confirm theme-specific styling
            if (isDarkTheme) {
                if (!hasImportantStyle(card, computedStyle, 'background')) {
                    card.style.background = '#2d3748';
                }
                if (!hasImportantStyle(card, computedStyle, 'borderColor')) {
                    card.style.borderColor = 'rgba(108, 92, 231, 0.2)';
                }
                if (!hasImportantStyle(card, computedStyle, 'boxShadow')) {
                    card.style.boxShadow = '0 6px 15px rgba(0, 0, 0, 0.15)';
                }
            } else {
                if (!hasImportantStyle(card, computedStyle, 'background')) {
                    card.style.background = 'white';
                }
                if (!hasImportantStyle(card, computedStyle, 'borderColor')) {
                    card.style.borderColor = 'rgba(108, 92, 231, 0.05)';
                }
                if (!hasImportantStyle(card, computedStyle, 'boxShadow')) {
                    card.style.boxShadow = '0 6px 15px rgba(0, 0, 0, 0.05)';
                }
            }
            
            // Add top gradient if missing
            if (!card.querySelector('.tag-card-gradient') && !hasInlineStyle(card, 'before')) {
                const gradient = document.createElement('div');
                gradient.className = 'tag-card-gradient';
                gradient.style.position = 'absolute';
                gradient.style.top = '0';
                gradient.style.left = '0';
                gradient.style.right = '0';
                gradient.style.height = '6px';
                gradient.style.background = 'linear-gradient(to right, #8e24aa, #e91e63)';
                gradient.style.zIndex = '1';
                card.prepend(gradient);
            }
        });
        
        // Fix tag card body
        document.querySelectorAll('.tag-card-body').forEach(body => {
            if (getComputedStyle(body).display !== 'flex') {
                body.style.padding = '1.75rem';
                body.style.flex = '1';
                body.style.display = 'flex';
                body.style.flexDirection = 'column';
                body.style.position = 'relative';
            }
        });
        
        // Fix tag card header
        document.querySelectorAll('.tag-card-header').forEach(header => {
            if (getComputedStyle(header).display !== 'flex') {
                header.style.display = 'flex';
                header.style.alignItems = 'flex-start';
                header.style.marginBottom = '1.25rem';
                header.style.position = 'relative';
            }
        });
        
        // Fix tag descriptions
        document.querySelectorAll('.tag-description').forEach(desc => {
            const computedStyle = getComputedStyle(desc);
            
            // Apply theme styles if not already applied via !important
            if (isDarkTheme) {
                if (!hasImportantStyle(desc, computedStyle, 'color')) {
                    desc.style.color = '#e2e8f0';
                }
                if (!hasImportantStyle(desc, computedStyle, 'backgroundColor')) {
                    desc.style.backgroundColor = '#374151';
                }
            } else {
                if (!hasImportantStyle(desc, computedStyle, 'color')) {
                    desc.style.color = '#4a5568';
                }
                if (!hasImportantStyle(desc, computedStyle, 'backgroundColor')) {
                    desc.style.backgroundColor = '#f8f9fa';
                }
            }
        });
        
        // Fix tag badges
        document.querySelectorAll('.tag-badge').forEach(badge => {
            const computedStyle = getComputedStyle(badge);
            
            // Apply theme styles if not already applied via !important
            if (isDarkTheme) {
                if (!hasImportantStyle(badge, computedStyle, 'backgroundColor')) {
                    badge.style.backgroundColor = 'rgba(187, 134, 252, 0.1)';
                }
                if (!hasImportantStyle(badge, computedStyle, 'color')) {
                    badge.style.color = '#bb86fc';
                }
                if (!hasImportantStyle(badge, computedStyle, 'boxShadow')) {
                    badge.style.boxShadow = '0 3px 10px rgba(187, 134, 252, 0.15)';
                }
            } else {
                if (!hasImportantStyle(badge, computedStyle, 'backgroundColor')) {
                    badge.style.backgroundColor = 'rgba(142, 36, 170, 0.1)';
                }
                if (!hasImportantStyle(badge, computedStyle, 'color')) {
                    badge.style.color = '#8e24aa';
                }
                if (!hasImportantStyle(badge, computedStyle, 'boxShadow')) {
                    badge.style.boxShadow = '0 3px 10px rgba(108, 92, 231, 0.1)';
                }
            }
        });
        
        // Fix tag counts
        document.querySelectorAll('.tag-count').forEach(count => {
            if (getComputedStyle(count).display !== 'flex') {
                count.style.display = 'flex';
                count.style.alignItems = 'center';
                count.style.gap = '0.3rem';
                count.style.padding = '0.3rem 0.7rem';
                count.style.fontSize = '0.8rem';
                count.style.fontWeight = '500';
                count.style.borderRadius = '50px';
                count.style.marginLeft = 'auto';
                
                if (isDarkTheme) {
                    count.style.backgroundColor = '#374151';
                    count.style.color = '#9ca3af';
                    count.style.borderColor = '#4b5563';
                } else {
                    count.style.backgroundColor = 'white';
                    count.style.color = '#6c757d';
                    count.style.border = '1px solid #dee2e6';
                }
            }
        });
        
        // Fix tag browse buttons
        document.querySelectorAll('.tag-browse-button').forEach(button => {
            const computedStyle = getComputedStyle(button);
            
            if (isDarkTheme) {
                if (!hasImportantStyle(button, computedStyle, 'backgroundColor')) {
                    button.style.backgroundColor = '#2d3748';
                }
                if (!hasImportantStyle(button, computedStyle, 'color')) {
                    button.style.color = '#e2e8f0';
                }
                if (!hasImportantStyle(button, computedStyle, 'boxShadow')) {
                    button.style.boxShadow = '0 3px 10px rgba(0, 0, 0, 0.2)';
                }
            } else {
                if (!hasImportantStyle(button, computedStyle, 'backgroundColor')) {
                    button.style.backgroundColor = '#f8f9fa';
                }
                if (!hasImportantStyle(button, computedStyle, 'color')) {
                    button.style.color = '#4a5568';
                }
                if (!hasImportantStyle(button, computedStyle, 'boxShadow')) {
                    button.style.boxShadow = '0 3px 10px rgba(108, 92, 231, 0.1)';
                }
            }
        });
        
        // Fix tag browse icons
        document.querySelectorAll('.tag-browse-button i').forEach(icon => {
            if (getComputedStyle(icon).marginBottom !== '0.5rem') {
                icon.style.fontSize = '1.5rem';
                icon.style.marginBottom = '0.5rem';
                icon.style.position = 'relative';
                icon.style.zIndex = '1';
                icon.style.transition = 'all 0.3s ease';
                
                if (isDarkTheme) {
                    icon.style.color = '#9ca3af';
                } else {
                    icon.style.color = '#6c757d';
                }
            }
        });
        
        // Fix tag action links
        document.querySelectorAll('.tag-action-link').forEach(link => {
            const computedStyle = getComputedStyle(link);
            
            if (link.classList.contains('primary')) {
                if (isDarkTheme) {
                    if (!hasImportantStyle(link, computedStyle, 'backgroundColor')) {
                        link.style.backgroundColor = 'rgba(187, 134, 252, 0.1)';
                    }
                    if (!hasImportantStyle(link, computedStyle, 'color')) {
                        link.style.color = '#bb86fc';
                    }
                } else {
                    if (!hasImportantStyle(link, computedStyle, 'backgroundColor')) {
                        link.style.backgroundColor = 'rgba(142, 36, 170, 0.1)';
                    }
                    if (!hasImportantStyle(link, computedStyle, 'color')) {
                        link.style.color = '#8e24aa';
                    }
                }
            } else {
                if (isDarkTheme) {
                    if (!hasImportantStyle(link, computedStyle, 'backgroundColor')) {
                        link.style.backgroundColor = '#2d3748';
                    }
                    if (!hasImportantStyle(link, computedStyle, 'color')) {
                        link.style.color = '#a0aec0';
                    }
                } else {
                    if (!hasImportantStyle(link, computedStyle, 'backgroundColor')) {
                        link.style.backgroundColor = 'white';
                    }
                    if (!hasImportantStyle(link, computedStyle, 'color')) {
                        link.style.color = '#6c757d';
                    }
                }
            }
        });
        
        // Fix tag card footer
        document.querySelectorAll('.tag-card-footer').forEach(footer => {
            if (getComputedStyle(footer).display !== 'flex') {
                footer.style.display = 'flex';
                footer.style.justifyContent = 'space-between';
                footer.style.paddingTop = '1.25rem';
                footer.style.marginTop = 'auto';
                
                if (isDarkTheme) {
                    footer.style.borderTop = '1px solid #4a5568';
                } else {
                    footer.style.borderTop = '1px solid #dee2e6';
                }
            }
        });
    }
    
    // Đảm bảo layout grid hiển thị đúng
    function refreshGridLayout() {
        const gridContainer = document.querySelector('.tags-grid-container');
        if (!gridContainer) return;
        
        const computedStyle = getComputedStyle(gridContainer);
        
        // Đảm bảo sử dụng grid layout
        if (computedStyle.display !== 'grid') {
            console.log('Fixing grid layout for tag container');
            gridContainer.style.display = 'grid';
            gridContainer.style.gridTemplateColumns = 'repeat(auto-fill, minmax(250px, 1fr))';
            gridContainer.style.gap = '1.5rem';
            gridContainer.style.width = '100%';
        }
        
        // Đếm số lượng card hiển thị
        const visibleCards = Array.from(gridContainer.children).filter(card => 
            window.getComputedStyle(card).display !== 'none'
        ).length;
        
        console.log(`Grid container has ${visibleCards} visible cards out of ${gridContainer.children.length} total`);
        
        // Nếu không có card nào hiển thị thì kiểm tra vấn đề
        if (visibleCards === 0 && gridContainer.children.length > 0) {
            console.warn('No cards are visible despite having children, forcing display');
            Array.from(gridContainer.children).forEach(card => {
                card.style.display = 'flex';
                card.style.flexDirection = 'column';
                card.style.height = '100%';
            });
        }
    }
    
    // Function to fix all styling issues
    function fixAllStyling() {
        console.log('Running all styling fixes...');
        
        // Đảm bảo grid layout trước khi fix các thành phần con
        refreshGridLayout();
        
        // Fix các thành phần tag
        fixTagPageStyling();
    }
    
    // Run fixes when DOM is loaded
    document.addEventListener('DOMContentLoaded', function() {
        // Wait a short time to ensure all elements are loaded
        setTimeout(fixAllStyling, 100);
        
        // Apply again after a longer time to catch any delayed rendering
        setTimeout(fixAllStyling, 500);
        
        // Run one more time after 1 second to ensure everything is fixed
        setTimeout(fixAllStyling, 1000);
        
        // Fix styling when theme changes
        const observer = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                if (mutation.attributeName === 'data-theme' || 
                    mutation.attributeName === 'data-bs-theme') {
                    console.log('Theme change detected, applying fixes...');
                    // Áp dụng fixes nhiều lần để đảm bảo tất cả hoạt động đúng
                    setTimeout(fixAllStyling, 100);
                    setTimeout(fixAllStyling, 300);
                    setTimeout(fixAllStyling, 500);
                }
            });
        });
        
        // Observe theme changes
        observer.observe(document.documentElement, { attributes: true });
        
        // Theo dõi thay đổi trong DOM để fix styling khi cần
        const contentObserver = new MutationObserver(mutations => {
            let needsRefresh = false;
            
            mutations.forEach(mutation => {
                if (mutation.type === 'childList' && 
                    (mutation.target.classList.contains('tags-grid-container') || 
                     mutation.target.closest('.tags-grid-container'))) {
                    needsRefresh = true;
                }
            });
            
            if (needsRefresh) {
                console.log('Content change detected in tag container, refreshing styles...');
                fixAllStyling();
            }
        });
        
        // Theo dõi thay đổi trong container
        const container = document.querySelector('.tags-grid-container');
        if (container) {
            contentObserver.observe(container, { 
                childList: true, 
                subtree: true,
                attributes: true,
                attributeFilter: ['style', 'class']
            });
        }
    });
    
    // Thêm sự kiện để khắc phục lỗi khi tương tác với trang
    document.addEventListener('click', function(e) {
        // Kích hoạt fixes khi click vào bất kỳ phần tử nào trong tag container
        if (e.target.closest('.tags-grid-container') || 
            e.target.closest('.tags-header') ||
            e.target.classList.contains('tags-title')) {
            console.log('User interaction detected, refreshing tag styling');
            fixAllStyling();
        }
    });
    
    // Expose to global scope
    window.themeFixUtils = {
        fixTagPageStyling,
        refreshGridLayout,
        fixAllStyling
    };
})(); 