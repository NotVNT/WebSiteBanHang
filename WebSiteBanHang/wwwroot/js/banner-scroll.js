document.addEventListener('DOMContentLoaded', function() {
    const container = document.querySelector('.banner-scroll-container');
    if (!container) return;
    
    const wrapper = container.querySelector('.banner-scroll-wrapper');
    const items = container.querySelectorAll('.banner-scroll-item');
    const dotsContainer = container.querySelector('.banner-scroll-dots');
    const prevBtn = container.querySelector('.banner-scroll-prev');
    const nextBtn = container.querySelector('.banner-scroll-next');
    
    let currentIndex = 0;
    let startX;
    let isDragging = false;
    let initialPosition;
    let autoScrollInterval;
    
    // Create dots
    items.forEach((_, index) => {
        const dot = document.createElement('div');
        dot.classList.add('banner-scroll-dot');
        if (index === 0) dot.classList.add('active');
        dot.addEventListener('click', () => goToSlide(index));
        dotsContainer.appendChild(dot);
    });
    
    // Set up navigation
    prevBtn.addEventListener('click', () => {
        clearAutoScroll();
        goToSlide(currentIndex - 1);
        startAutoScroll();
    });
    
    nextBtn.addEventListener('click', () => {
        clearAutoScroll();
        goToSlide(currentIndex + 1);
        startAutoScroll();
    });
    
    // Touch events for mobile
    wrapper.addEventListener('touchstart', handleTouchStart, { passive: true });
    wrapper.addEventListener('touchmove', handleTouchMove, { passive: true });
    wrapper.addEventListener('touchend', handleTouchEnd);
    
    // Mouse events for desktop
    wrapper.addEventListener('mousedown', handleDragStart);
    wrapper.addEventListener('mousemove', handleDragMove);
    wrapper.addEventListener('mouseup', handleDragEnd);
    wrapper.addEventListener('mouseleave', handleDragEnd);
    
    function handleTouchStart(e) {
        startX = e.touches[0].clientX;
        handleDragStart(e);
    }
    
    function handleTouchMove(e) {
        if (!startX) return;
        let currentX = e.touches[0].clientX;
        let diff = startX - currentX;
        
        // Capture the drag with the current touch position
        e.clientX = currentX;
        handleDragMove(e);
    }
    
    function handleTouchEnd(e) {
        handleDragEnd(e);
        startX = null;
    }
    
    function handleDragStart(e) {
        clearAutoScroll();
        isDragging = true;
        initialPosition = getPositionX(wrapper);
        startX = getEventX(e);
        
        // Change cursor and prevent default behavior
        wrapper.style.cursor = 'grabbing';
        e.preventDefault();
    }
    
    function handleDragMove(e) {
        if (!isDragging) return;
        
        const currentX = getEventX(e);
        const diff = (startX - currentX);
        
        setTransform(wrapper, initialPosition - diff);
    }
    
    function handleDragEnd(e) {
        if (!isDragging) return;
        
        isDragging = false;
        const movedBy = initialPosition - getPositionX(wrapper);
        
        // Determine if slide should change based on movement
        if (movedBy > 100) {
            goToSlide(currentIndex + 1);
        } else if (movedBy < -100) {
            goToSlide(currentIndex - 1);
        } else {
            goToSlide(currentIndex);
        }
        
        wrapper.style.cursor = 'grab';
        startAutoScroll();
    }
    
    function getEventX(e) {
        return e.type.includes('mouse') ? e.clientX : e.touches[0].clientX;
    }
    
    function getPositionX(element) {
        const style = window.getComputedStyle(element);
        const matrix = new WebKitCSSMatrix(style.transform);
        return matrix.m41;
    }
    
    function setTransform(element, position) {
        element.style.transform = `translateX(${position}px)`;
    }
    
    function goToSlide(index) {
        // Handle circular navigation
        if (index < 0) {
            index = items.length - 1;
        } else if (index >= items.length) {
            index = 0;
        }
        
        currentIndex = index;
        const position = -index * container.offsetWidth;
        
        // Smooth transition to the slide
        wrapper.style.transition = 'transform 0.5s ease';
        setTransform(wrapper, position);
        
        // Update active dot
        const dots = dotsContainer.querySelectorAll('.banner-scroll-dot');
        dots.forEach((dot, i) => {
            dot.classList.toggle('active', i === index);
        });
        
        // Reset transition after slide change
        setTimeout(() => {
            wrapper.style.transition = '';
        }, 500);
    }
    
    // Automatic scrolling
    function startAutoScroll() {
        clearAutoScroll();
        autoScrollInterval = setInterval(() => {
            goToSlide(currentIndex + 1);
        }, 5000); // Change slide every 5 seconds
    }
    
    function clearAutoScroll() {
        if (autoScrollInterval) {
            clearInterval(autoScrollInterval);
        }
    }
    
    // Handle window resize
    window.addEventListener('resize', () => {
        goToSlide(currentIndex);
    });
    
    // Start auto-scrolling
    startAutoScroll();
});
