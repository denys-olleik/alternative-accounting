// UniversalSelector.js

// Utility function to load the template HTML file asynchronously
function loadTemplate(url) {
  return fetch(url).then(response => response.text());
}

// Factory function that returns a Promise resolving to the component definition
function createUniversalSelectorComponent() {
  return loadTemplate('/js/UniversalSelector.html').then(templateHtml => ({
    name: 'UniversalSelector',
    template: templateHtml,
    props: {
      items: {
        type: Array,
        required: true
      },
      selectedItems: {
        type: Array,
        default: () => []
      },
      label: {
        type: String,
        default: ''
      },
      placeholder: {
        type: String,
        default: ''
      },
      itemKey: {
        type: String,
        default: 'id'
      },
      itemLabel: {
        type: String,
        default: 'name'
      }
    },
    emits: ['update:selectedItems'],
    data() {
      return {
        inputText: '',
        preselectedIndex: 0,
        dropdownVisible: false,
        remember: false
      };
    },
    computed: {
      filteredItems() {
        const lower = this.inputText.toLowerCase();
        return this.items
          .filter(item =>
            !this.selectedItems.some(sel => this.getItemKey(sel) === this.getItemKey(item)) &&
            this.getItemLabel(item).toLowerCase().includes(lower)
          );
      }
    },
    methods: {
      getItemKey(item) {
        return item[this.itemKey];
      },
      getItemLabel(item) {
        return item[this.itemLabel];
      },
      handleFocus() {
        this.dropdownVisible = true;
        this.preselectedIndex = 0;
      },
      handleBlur() {
        setTimeout(() => {
          this.dropdownVisible = false;
        }, 200);
      },
      handleKeyDown(event) {
        if (!this.dropdownVisible && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
          this.dropdownVisible = true;
          return;
        }
        if (event.key === 'ArrowDown') {
          event.preventDefault();
          if (this.preselectedIndex < this.filteredItems.length - 1) {
            this.preselectedIndex++;
          } else {
            this.preselectedIndex = 0;
          }
        } else if (event.key === 'ArrowUp') {
          event.preventDefault();
          if (this.preselectedIndex > 0) {
            this.preselectedIndex--;
          } else {
            this.preselectedIndex = this.filteredItems.length - 1;
          }
        } else if (event.key === 'Enter') {
          event.preventDefault();
          if (this.filteredItems.length > 0) {
            this.selectItem(this.filteredItems[this.preselectedIndex]);
          }
        }
      },
      selectItem(item) {
        const updated = [...this.selectedItems, item];
        this.$emit('update:selectedItems', updated);
        this.inputText = '';
        this.dropdownVisible = false;
        this.preselectedIndex = 0;
      },
      removeItem(item) {
        const updated = this.selectedItems.filter(sel => this.getItemKey(sel) !== this.getItemKey(item));
        this.$emit('update:selectedItems', updated);
      }
    }
  }));
}

// Usage in your main app setup (example):
// createUniversalSelectorComponent().then(UniversalSelector => {
//   app.component('universal-selector', UniversalSelector);
//   app.mount('#app');
// });