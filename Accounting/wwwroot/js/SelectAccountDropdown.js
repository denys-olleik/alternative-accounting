const SelectAccountDropdown = {
	name: 'SelectAccountDropdown',
	template: `
		<div class="display-flex flex-direction-column">
			<input ref="inputElement" type="text" class="font-size-20px font-Roboto-Mono"
				@keydown="handleKeyDown"
				@focus="handleFocus"
				@blur="handleBlur"
				v-model="inputText"
			/>
			<div class="display-flex flex-direction-column background-color-black">
				<div v-if="selectedAccount && !dropdownVisible" class="display-flex font-size-16px margin-5px font-Roboto-Mono">
					<div>
						<span class="margin-right-10px color-lawngreen">█</span>
					</div>
					<div class="color-white">
						{{ selectedAccount.name }}
					</div>
				</div>
			</div>
			<span>
				<input type="checkbox" v-model="remember" class="margin-bottom-5px" />
				<label>remember</label>
			</span>
			<div v-if="dropdownVisible" class="display-flex flex-direction-column background-color-black font-Roboto-Mono padding-5px">
				<div v-for="(account, index) in filteredAccounts" :key="account.accountID"
					@click="handleAccountSelected(account)"
					class="display-flex font-size-16px color-white"
				>
					<div>
						<span :class="['margin-right-10px', account.accountID === selectedAccount?.accountID ? 'color-lawngreen' : '', index === preselectedIndex ? 'blinking-cursor' : '']">█</span>
					</div>
					<div>
						{{ account.name }}
					</div>
				</div>
			</div>
		</div>
	`,
	props: {
		accounts: {
			type: Array,
			required: true
		},
		selectedAccount: {
			type: Object,
			default: null
		}
	},
	emits: ['account-selected'],
	data() {
		return {
			inputText: '',
			preselectedIndex: 0,
			dropdownVisible: false,
			remember: false
		};
	},
	watch: {
		inputText(newVal) {
			if (newVal !== '') {
				this.dropdownVisible = true;
			}
		}
	},
	computed: {
		filteredAccounts() {
			if (!this.inputText) {
				return this.accounts;
			}
			return this.accounts.filter(account =>
				account.name.toLowerCase().includes(this.inputText.toLowerCase())
			);
		}
	},
	methods: {
		handleBlur() {
			setTimeout(() => {
				this.dropdownVisible = false;
			}, 200);
		},
		handleFocus() {
			if (this.selectedAccount) {
				this.preselectedIndex = this.filteredAccounts.findIndex(account =>
					account.accountID === this.selectedAccount.accountID
				);
			} else {
				this.preselectedIndex = 0;
			}
			this.dropdownVisible = true;
		},
		handleKeyDown(event) {
			if (event.key === 'Enter') {
				event.preventDefault();

				if (!this.dropdownVisible) {
					this.dropdownVisible = true;
					return;
				}

				if (this.preselectedIndex < this.filteredAccounts.length) {
					this.handleAccountSelected(this.filteredAccounts[this.preselectedIndex]);
					this.dropdownVisible = false;
				} else {
					let apiUrl = `/api/a/create`;

					let account = { accountName: this.inputText };

					fetch(apiUrl, {
						method: 'POST',
						headers: { 'Content-Type': 'application/json' },
						body: JSON.stringify(account)
					})
						.then(response => response.json())
						.then(data => {
							this.$emit('account-selected', data);
							this.dropdownVisible = false;
						})
						.catch((error) => {
							console.error('Error:', error);
						});

					this.inputText = '';
				}
			} else if (event.key === 'ArrowUp') {
				event.preventDefault();
				if (this.preselectedIndex > 0) {
					this.preselectedIndex--;
				} else {
					this.preselectedIndex = this.filteredAccounts.length - 1;
				}
				this.dropdownVisible = true;
			} else if (event.key === 'ArrowDown') {
				event.preventDefault();
				if (this.preselectedIndex < this.filteredAccounts.length - 1) {
					this.preselectedIndex++;
				} else {
					this.preselectedIndex = 0;
				}
				this.dropdownVisible = true;
			}
		},
		handleAccountSelected(account) {
			this.inputText = '';
			this.$emit('account-selected', account);
		},
	}
};